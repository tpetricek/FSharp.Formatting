// can't yet format YamlFrontmatter (["title: Markdown Content"; "category: Examples"; "categoryindex: 2"; "index: 2"], Some { StartLine = 1 StartColumn = 0 EndLine = 5 EndColumn = 8 }) to pynb markdown

# Example: Using Markdown Content

This file demonstrates how to write Markdown document with
embedded F# snippets that can be transformed into nice HTML
using the `literate.fsx` script from the [F# Formatting
package](http://fsprojects.github.io/FSharp.Formatting).

In this case, the document itself is a valid Markdown and
you can use standard Markdown features to format the text:

// can't yet format Span ([Literal ("Here is an example of unordered list and...", Some { StartLine = 17 StartColumn = 3 EndLine = 17 EndColumn = 46 })], Some { StartLine = 17 StartColumn = 0 EndLine = 17 EndColumn = 46 }) to pynb markdown
// can't yet format Span ([Literal ("Text formatting including ", Some { StartLine = 18 StartColumn = 3 EndLine = 19 EndColumn = 29 }); Strong ([Literal ("bold", Some { StartLine = 18 StartColumn = 29 EndLine = 19 EndColumn = 33 })], Some { StartLine = 18 StartColumn = 29 EndLine = 19 EndColumn = -1 }); Literal (" and ", Some { StartLine = 18 StartColumn = 29 EndLine = 19 EndColumn = 34 }); Emphasis ([Literal ("emphasis", Some { StartLine = 18 StartColumn = 34 EndLine = 19 EndColumn = 42 })], Some { StartLine = 18 StartColumn = 34 EndLine = 19 EndColumn = -1 })], Some { StartLine = 17 StartColumn = 0 EndLine = 17 EndColumn = 46 }) to pynb markdown
For more information, see the [Markdown](http://daringfireball.net/projects/markdown) reference.

## Writing F# code

In standard Markdown, you can include code snippets by
writing a block indented by four spaces and the code
snippet will be turned into a `<pre>` element. If you do
the same using Literate F# tool, the code is turned into
a nicely formatted F# snippet:

// can't yet format InlineHtmlBlock ("/// The Hello World of functional languages!
let rec factorial x = 
  if x = 0 then 1 
  else x * (factorial (x - 1))

let f10 = factorial 10
", None, None) to pynb markdown

## Hiding code

If you want to include some code in the source code,
but omit it from the output, you can use the `hide`
command. You can also use `module=...` to specify that
the snippet should be placed in a separate module
(e.g. to avoid duplicate definitions).

The value will be deffined in the F# code that is
processed and so you can use it from other (visible)
code and get correct tool tips:

// can't yet format InlineHtmlBlock ("let answer = Hidden.answer
", None, None) to pynb markdown

## Including other snippets

When writing literate programs as Markdown documents,
you can also include snippets in other languages.
These will not be colorized and processed as F#
code samples:

// can't yet format InlineHtmlBlock ("Console.WriteLine("Hello world!");
", None, None) to pynb markdown

This snippet is turned into a `pre` element with the
`lang` attribute set to `csharp`.


