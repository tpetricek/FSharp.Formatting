// --------------------------------------------------------------------------------------
// F# CodeFormat (CommentProcessing.fs)
// (c) Tomas Petricek, 2012, Available under Apache 2.0 license.
// --------------------------------------------------------------------------------------
module private FSharp.Formatting.CodeFormat.CommentFilter

open System.Text
open FSharp.Patterns
open FSharp.Collections
open FSharp.Formatting.Markdown
open FSharp.Compiler.Tokenization

// --------------------------------------------------------------------------------------
// Handle special comments that can appear in F# snipptes. This includes:
// Marking of a snippet that should be formatted:
//
//     // [snippet:Some name]
//     // [/snippet]
//
// Omitting of a block of code in a visible snippet:
//
//     (*[omit:<replacement>]*)...(*[/omit]*)
//
// Displaying of F# interactive output:
//
//     // [fsi:<here is some fsi output>]
//
// --------------------------------------------------------------------------------------

type Token = string * FSharpTokenInfo
type SnippetLine = Token list
type IndexedSnippetLine = MarkdownRange * SnippetLine
type Snippet = IndexedSnippetLine list
type NamedSnippet = string * Snippet

/// Finds special commands (comments) in the source code. If there are commands, then
/// we only generate HTML for parts of source (snippets). This function returns a list
/// of snippets. The commands should be:
///    // [snippet:Some title]
///    ... some F# code ...
///    // [/snippet]
let rec getSnippets (state:NamedSnippet option) (snippets:NamedSnippet list)
                    (source:IndexedSnippetLine list) (lines:string[]) =
  match source with
  | [] -> snippets
  | (line, tokens)::rest ->
    let text = lines.[line.StartLine].Trim(), line
    match state, text with

    // We're not inside a snippet and we found a beginning of one
    | None, StringPosition.StartsWithTrim "//" (StringPosition.StartsWithTrim "[snippet:" title) ->
        let title = (fst title).Substring(0, (fst title).IndexOf(']'))
        getSnippets (Some(title, [])) snippets rest lines
    // Not inside a snippet and there is a usual line
    | None, _ ->
        getSnippets state snippets rest lines

    // We're inside a snippet and it ends
    | Some(title, acc), StringPosition.StartsWithTrim "//" (StringPosition.StartsWithTrim "[/snippet]" _) ->
        getSnippets None ((title, acc |> List.rev)::snippets) rest lines
    // We're inside snippet - add current line to it
    | Some(title, acc), _ ->
        getSnippets (Some(title, (line, tokens)::acc)) snippets rest lines


/// Preprocesses a line and merges all subsequent comments on a line
/// into a single long comment (so that we can parse it as snippet command)
let rec mergeComments (line:SnippetLine) (cmt:Token option) (acc:SnippetLine) =
  match line, cmt with
  | [], Some(cmt) -> cmt::acc |> List.rev
  | [], None -> acc |> List.rev
  | (str, tok)::line, None when tok.TokenName = "COMMENT" || tok.TokenName = "LINE_COMMENT" ->
      mergeComments line (Some(str, tok)) acc
  | (str, tok)::line, Some(scmt, cmt) when tok.TokenName = "COMMENT" || tok.TokenName = "LINE_COMMENT"->
      let ncmt = {cmt with RightColumn = tok.RightColumn }
      mergeComments line (Some(scmt+str, ncmt)) acc
  | (str, tok)::line, None ->
      mergeComments line None ((str, tok)::acc)
  | (str, tok)::line, Some(cmt) ->
      mergeComments line None ((str, tok)::cmt::acc)


/// Continue reading shrinked code until we reach the end (*[/omit]*) tag
/// (see the function below for more information and beginning of shrinking)
let rec shrinkOmittedCode (text:StringBuilder) line content (source:Snippet) =
  match content, source with
  // Take the next line, merge comments and continue looking for end
  | [], (line, content)::source ->
      shrinkOmittedCode (text.Append("\n")) line (mergeComments content None []) source
  | (String.StartsAndEndsWithTrim ("(*", "*)") "[/omit]", tok)::rest, source
        when tok.TokenName = "COMMENT" ->
      line, rest, source, text
  | (str, _tok)::rest, _ ->
      shrinkOmittedCode (text.Append(str)) line rest source
  | [], [] -> line, [], [], text


/// Find all code marked using the (*[omit:<...>]*) tags and replace it with
/// a special token (named "OMIT...." where "...." is a replacement string)
let rec shrinkLine line (content:SnippetLine) (source:Snippet) =
  match content with
  | [] -> [], source
  | (String.StartsAndEndsWithTrim ("(*", "*)") (String.StartsAndEndsWithTrim ("[omit:", "]") body), (tok:FSharpTokenInfo))::rest
        when tok.TokenName = "COMMENT" ->
      let line, remcontent, source, text =
        shrinkOmittedCode (StringBuilder()) line rest source
      let line, source = shrinkLine line remcontent source
      (body, { tok with TokenName = "OMIT" + (text.ToString()) })::line, source
  | (String.StartsWithTrim "//" (String.StartsAndEndsWith ("[fsi:", "]") fsi), (tok:FSharpTokenInfo))::rest ->
      let line, source = shrinkLine line rest source
      (fsi, { tok with TokenName = "FSI"})::line, source
  | (str, tok)::rest ->
      let line, source = shrinkLine line rest source
      (str, tok)::line, source

/// Process the whole source file and shrink all blocks marked using
/// special 'omit' meta-comments (see the two functions above)
let rec shrinkOmittedParts (source:Snippet) : Snippet =
  [ match source with
    | [] -> ()
    | (line, content)::source ->
      let content, source = shrinkLine line (mergeComments content None []) source
      yield line, content
      yield! shrinkOmittedParts source ]

