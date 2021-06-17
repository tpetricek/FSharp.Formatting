// can't yet format YamlFrontmatter (["category: Advanced"; "categoryindex: 3"; "index: 2"], Some { StartLine = 2 StartColumn = 0 EndLine = 5 EndColumn = 8 }) to pynb markdown

[![Binder](img/badge-binder.svg)](https://mybinder.org/v2/gh/diffsharp/diffsharp.github.io/master?filepath=markdown.ipynb)&emsp;
[![Script](img/badge-script.svg)](markdown.fsx)&emsp;
[![Notebook](img/badge-notebook.svg)](markdown.ipynb)

# Markdown parser

This page demonstrates how to use `FSharp.Formatting.Markdown` to parse a Markdown
document, process the obtained document representation and
how to turn the code into a nicely formatted HTML.

First, we need to load the assembly and open necessary namespaces:

// can't yet format InlineHtmlBlock ("open FSharp.Formatting.Markdown
open FSharp.Formatting.Common", None, None) to pynb markdown

## Parsing documents

The F# Markdown parser recognizes the standard [Markdown syntax](http://daringfireball.net/projects/markdown/)
and it is not the aim of this tutorial to fully document it.
The following snippet creates a simple string containing a document
with several elements and then parses it using the [Markdown.Parse](https://fsprojects.github.io/FSharp.Formatting/reference/fsharp-formatting-markdown-markdown.html) method:

// can't yet format InlineHtmlBlock ("let document = """
# F# Hello world
Hello world in [F#](http://fsharp.net) looks like this:

    printfn "Hello world!"

For more see [fsharp.org][fsorg].

  [fsorg]: http://fsharp.org "The F# organization." """

let parsed = Markdown.Parse(document)", None, None) to pynb markdown

The sample document consists of a first-level heading (written using
one of the two alternative styles) followed by a paragraph with a
**direct** link, code snippet and one more paragraph that includes an
**indirect** link. The URLs of indirect links are defined by a separate
block as demonstrated on the last line (and they can then be easily used repeatedly
from multiple places in the document).

## Working with parsed documents

The F# Markdown processor does not turn the document directly into HTML.
Instead, it builds a nice F# data structure that we can use to analyze,
transform and process the document. First of all the [MarkdownDocument.DefinedLinks](https://fsprojects.github.io/FSharp.Formatting/reference/fsharp-formatting-markdown-markdowndocument.html#DefinedLinks) property
returns all indirect link definitions:

// can't yet format InlineHtmlBlock ("parsed.DefinedLinks
// [fsi:val it : IDictionary<string,(string * string option)> =]
// [fsi:  dict [("fsorg", ("http://fsharp.org", Some "The F# organization."))]]", None, None) to pynb markdown

The document content can be accessed using the [MarkdownDocument.Paragraphs](https://fsprojects.github.io/FSharp.Formatting/reference/fsharp-formatting-markdown-markdowndocument.html#Paragraphs) property that returns
a sequence of paragraphs or other first-level elements (headings, quotes, code snippets, etc.).
The following snippet prints the heading of the document:

// can't yet format InlineHtmlBlock ("// Iterate over all the paragraph elements
for par in parsed.Paragraphs do
  match par with
  | Heading(size=1; body=[Literal(text=text)]) -> 
      // Recognize heading that has a simple content
      // containing just a literal (no other formatting)
      printfn "%s" text
  | _ -> ()", None, None) to pynb markdown

You can find more detailed information about the document structure and how to process it
in the book [F# Deep Dives](http://manning.com/petricek2/).

## Processing the document recursively

The library provides active patterns that can be used to easily process the Markdown
document recursively. The example in this section shows how to extract all links from the
document. To do that, we need to write two recursive functions. One that will process
all paragraph-style elements and one that will process all inline formattings (inside
paragraphs, headings etc.).

To avoid pattern matching on every single kind of span and every single kind of
paragraph, we can use active patterns from the [MarkdownPatterns](https://fsprojects.github.io/FSharp.Formatting/reference/fsharp-formatting-markdown-markdownpatterns.html) module. These can be use
to recognize any paragraph or span that can contain child elements:

// can't yet format InlineHtmlBlock ("/// Returns all links in a specified span node
let rec collectSpanLinks span = seq {
  match span with
  | DirectLink(link=url) -> yield url
  | IndirectLink(key=key) -> yield fst (parsed.DefinedLinks.[key])
  | MarkdownPatterns.SpanLeaf _ -> ()
  | MarkdownPatterns.SpanNode(_, spans) ->
      for s in spans do yield! collectSpanLinks s }
      
/// Returns all links in the specified paragraph node
let rec collectParLinks par = seq {
  match par with
  | MarkdownPatterns.ParagraphLeaf _ -> ()
  | MarkdownPatterns.ParagraphNested(_, pars) -> 
      for ps in pars do 
        for p in ps do yield! collectParLinks p 
  | MarkdownPatterns.ParagraphSpans(_, spans) ->
      for s in spans do yield! collectSpanLinks s }

/// Collect links in the entire document
Seq.collect collectParLinks parsed.Paragraphs
// [fsi:val it : seq<string> =]
// [fsi:  seq ["http://fsharp.net"; "http://fsharp.org"]]", None, None) to pynb markdown

The `collectSpanLinks` function works on individual span elements that contain inline
formatting (emphasis, strong) and also links. The `DirectLink` node from [MarkdownSpan](https://fsprojects.github.io/FSharp.Formatting/reference/fsharp-formatting-markdown-markdownspan.html) represents an inline
link like the one pointing to [http://fsharp.net](http://fsharp.net) while `IndirectLink` represents a
link that uses one of the link definitions. The function simply returns the URL associated
with the link.

Some span nodes (like emphasis) can contain other formatting, so we need to recursively
process children. This is done by matching against `MarkdownPatterns.SpanNodes` which is an active
pattern that recognizes any node with children. The library also provides a **function**
named `MarkdownPatterns.SpanNode` that can be used to reconstruct the same node (when you want
to transform document). This is similar to how the `ExprShape` module for working with
F# quotations works.

The function `collectParLinks` processes paragraphs - a paragraph cannot directly be a
link so we just need to process all spans. This time, there are three options.
`ParagraphLeaf` represents a case where the paragraph does not contain any spans
(a code block or, for example, a `<hr>` line); the `ParagraphNested` case is used for paragraphs
that contain other paragraphs (quotation) and `ParagraphSpans` is used for all other
paragraphs that contain normal text - here we call `collectSpanLinks` on all nested spans.

## Generating HTML output

Finally, the [Markdown](https://fsprojects.github.io/FSharp.Formatting/reference/fsharp-formatting-markdown-markdown.html) type also includes a method [Markdown.ToHtml](https://fsprojects.github.io/FSharp.Formatting/reference/fsharp-formatting-markdown-markdown.html) that can be used
to generate an HTML document from the Markdown input. The following example shows how to call it:

// can't yet format InlineHtmlBlock ("let html = Markdown.ToHtml(parsed)", None, None) to pynb markdown

There are also methods to generate `.fsx`, `.ipynb`, `.md` and `.tex`.


