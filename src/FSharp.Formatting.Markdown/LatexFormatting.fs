// --------------------------------------------------------------------------------------
// F# Markdown (LatexFormatting.fs)
// (c) Tomas Petricek, 2012, Available under Apache 2.0 license.
// --------------------------------------------------------------------------------------

/// [omit]
module internal FSharp.Formatting.Markdown.LatexFormatting

open System.IO
open System.Web
open System.Collections.Generic
open FSharp.Patterns
open FSharp.Collections
open MarkdownUtils

/// LaTEX special chars
/// from http://tex.stackexchange.com/questions/34580/escape-character-in-latex
let specialChars =
    [|  // This line comes first to avoid double replacing
        // It also accommodates \r, \n, \t, etc.
        @"\", @"<\textbackslash>";
        "#", @"\#";
        "$", @"\$";
        "%", @"\%";
        "&", @"\&";
        "_", @"\_";
        "{", @"\{";
        "}", @"\}";
        @"<\textbackslash>", @"{\textbackslash}";
        "~",@"{\textasciitilde}";
        "^", @"{\textasciicircum}" |]

let latexEncode s =
    specialChars |> Array.fold (fun (acc:string) (k, v) -> acc.Replace(k, v)) (System.Net.WebUtility.HtmlDecode s)

/// Lookup a specified key in a dictionary, possibly
/// ignoring newlines or spaces in the key.
let (|LookupKey|_|) (dict:IDictionary<_, _>) (key:string) =
  [ key; key.Replace("\r\n", ""); key.Replace("\r\n", " ");
    key.Replace("\n", ""); key.Replace("\n", " ") ]
  |> Seq.tryPick (fun key ->
    match dict.TryGetValue(key) with
    | true, v -> Some v
    | _ -> None)

/// Context passed around while formatting the LaTEX
type FormattingContext =
  { LineBreak : unit -> unit
    Newline : string
    Writer : TextWriter
    Links : IDictionary<string, string * option<string>> }

let smallBreak (ctx:FormattingContext) () =
  ctx.Writer.Write(ctx.Newline)
let noBreak (_ctx:FormattingContext) () = ()

/// Write MarkdownSpan value to a TextWriter
let rec formatSpan (ctx:FormattingContext) = function
  | LatexInlineMath(body, _) -> ctx.Writer.Write(sprintf "$%s$" body)
  | LatexDisplayMath(body, _) -> ctx.Writer.Write(sprintf "$$%s$$" body)
  | EmbedSpans(cmd, _) -> formatSpans ctx (cmd.Render())
  | Literal(str, _) -> ctx.Writer.Write(latexEncode str)
  | HardLineBreak(_) -> ctx.LineBreak(); ctx.LineBreak()

  | AnchorLink _ -> ()
  | IndirectLink(body, _, LookupKey ctx.Links (link, _), _)
  | DirectLink(body, link, _, _)
  | IndirectLink(body, link, _, _) ->
      ctx.Writer.Write(@"\href{")
      ctx.Writer.Write(latexEncode link)
      ctx.Writer.Write("}{")
      formatSpans ctx body
      ctx.Writer.Write("}")

  | IndirectImage(body, _, LookupKey ctx.Links (link, _), _)
  | DirectImage(body, link, _, _)
  | IndirectImage(body, link, _, _) ->
      // Use the technique introduced at
      // http://stackoverflow.com/q/14014827
      if not (System.String.IsNullOrWhiteSpace(body)) then
        ctx.Writer.Write(@"\begin{figure}[htbp]\centering")
        ctx.LineBreak()
      ctx.Writer.Write(@"\includegraphics[width=1.0\textwidth]{")
      ctx.Writer.Write(latexEncode link)
      ctx.Writer.Write("}")
      ctx.LineBreak()
      if not (System.String.IsNullOrWhiteSpace(body)) then
        ctx.Writer.Write(@"\caption{")
        ctx.Writer.Write(latexEncode body)
        ctx.Writer.Write("}")
        ctx.LineBreak()
        ctx.Writer.Write(@"\end{figure}")
        ctx.LineBreak()

  | Strong(body, _) ->
      ctx.Writer.Write(@"\textbf{")
      formatSpans ctx body
      ctx.Writer.Write("}")
  | InlineCode(body, _) ->
      ctx.Writer.Write(@"\texttt{")
      ctx.Writer.Write(latexEncode body)
      ctx.Writer.Write("}")
  | Emphasis(body, _) ->
      ctx.Writer.Write(@"\emph{")
      formatSpans ctx body
      ctx.Writer.Write("}")

/// Write list of MarkdownSpan values to a TextWriter
and formatSpans ctx = List.iter (formatSpan ctx)

/// Write a MarkdownParagraph value to a TextWriter
let rec formatParagraph (ctx:FormattingContext) paragraph =
  match paragraph with
  | LatexBlock(env, lines, _) ->
    ctx.LineBreak(); ctx.LineBreak()
    ctx.Writer.Write(sprintf @"\begin{%s}" env); ctx.LineBreak()
    for line in lines do
      ctx.Writer.Write(line)
      ctx.LineBreak()
    ctx.Writer.Write(sprintf @"\end{%s}" env)
    ctx.LineBreak(); ctx.LineBreak()

  | EmbedParagraphs(cmd, _) -> formatParagraphs ctx (cmd.Render())
  | Heading(n, spans, _) ->
      let level =
        match n with
        | 1 -> @"\section*"
        | 2 -> @"\subsection*"
        | 3 -> @"\subsubsection*"
        | 4 -> @"\paragraph"
        | 5 -> @"\subparagraph"
        | _ -> ""
      ctx.Writer.Write(level + "{")
      formatSpans ctx spans
      ctx.Writer.Write("}")
      ctx.LineBreak()
  | Paragraph(spans, _) ->
      ctx.LineBreak(); ctx.LineBreak()
      for span in spans do
        formatSpan ctx span

  | HorizontalRule(_) ->
      // Reference from http://tex.stackexchange.com/q/19579/9623
      ctx.Writer.Write(@"\noindent\makebox[\linewidth]{\rule{\linewidth}{0.4pt}}\medskip")
      ctx.LineBreak()

  | CodeBlock (code, _, _, _, _) ->
      ctx.Writer.Write(@"\begin{lstlisting}")
      ctx.LineBreak()
      ctx.Writer.Write(code)
      ctx.LineBreak()
      ctx.Writer.Write(@"\end{lstlisting}")
      ctx.LineBreak()

  | OutputBlock (code, _kind, _executionCount) -> // TODO: could format output better using kind
      ctx.Writer.Write(@"\begin{lstlisting}")
      ctx.LineBreak()
      ctx.Writer.Write(code)
      ctx.LineBreak()
      ctx.Writer.Write(@"\end{lstlisting}")
      ctx.LineBreak()

  | TableBlock(headers, alignments, rows, _) ->
      let aligns = alignments |> List.map (function
        | AlignRight -> "|r"
        | AlignCenter -> "|c"
        | AlignDefault | AlignLeft -> "|l") |> String.concat ""
      ctx.Writer.Write(@"\begin{tabular}{" + aligns + @"|}\hline")
      ctx.LineBreak()

      let bodyCtx = { ctx with LineBreak = noBreak ctx }
      let formatRow (prefix:string) (postfix:string) row =
        row |> Seq.iteri (fun i cell ->
          if i <> 0 then ctx.Writer.Write(" & ")
          ctx.Writer.Write(prefix)
          cell |> List.iter (formatParagraph bodyCtx)
          ctx.Writer.Write(postfix) )

      for header in Option.toList headers do
        formatRow @"\textbf{" "}" header
        ctx.Writer.Write(@"\\ \hline\hline")
        ctx.LineBreak()
      for row in rows do
        formatRow "" "" row
        ctx.Writer.Write(@"\\ \hline")
        ctx.LineBreak()
      ctx.Writer.Write(@"\end{tabular}")
      ctx.LineBreak()

  | ListBlock(kind, items, _) ->
      let tag = if kind = Ordered then "enumerate" else "itemize"
      ctx.Writer.Write(@"\begin{" + tag + "}")
      ctx.LineBreak()
      for body in items do
        ctx.Writer.Write(@"\item ")
        body |> List.iter (formatParagraph ctx)
        ctx.LineBreak()
      ctx.Writer.Write(@"\end{" + tag + "}")
      ctx.LineBreak()

  | QuotedBlock(body, _) ->
      ctx.Writer.Write(@"\begin{quote}")
      ctx.LineBreak()
      formatParagraphs ctx body
      ctx.Writer.Write(@"\end{quote}")
      ctx.LineBreak()

  | Span(spans, _) ->
      formatSpans ctx spans
  | InlineHtmlBlock(code, _executionCount, _) ->
      ctx.Writer.Write(code)
  | OtherBlock (code, _) ->
      ctx.Writer.Write(@"\begin{lstlisting}")
      ctx.LineBreak()
      for (code, _) in code do
          ctx.Writer.Write(code)
      ctx.LineBreak()
      ctx.Writer.Write(@"\end{lstlisting}")
      ctx.LineBreak()
  | YamlFrontmatter (_lines, _) -> ()
  ctx.LineBreak()

/// Write a list of MarkdownParagraph values to a TextWriter
and formatParagraphs ctx paragraphs =
  let length = List.length paragraphs
  let ctx = { ctx with LineBreak = smallBreak ctx }
  for _last, paragraph in paragraphs |> Seq.mapi (fun i v -> (i = length - 1), v) do
    formatParagraph ctx paragraph

/// Format Markdown document and write the result to
/// a specified TextWriter. Parameters specify newline character
/// and a dictionary with link keys defined in the document.
let formatMarkdown writer links replacements newline crefResolver paragraphs =
  let ctx = { Links = links; Substitutions=replacements; Newline=newline; ResolveApiDocReference=crefResolver; DefineSymbol="LATEX" }
  let paragraphs = applySubstitutionsInMarkdown ctx paragraphs
  formatParagraphs
    { Writer = writer
      Links = links
      Newline = newline
      LineBreak = ignore }
    paragraphs