// can't yet format YamlFrontmatter (["title: Literate Script"; "category: Examples"; "categoryindex: 2"; "index: 1"], Some { StartLine = 2 StartColumn = 0 EndLine = 6 EndColumn = 8 }) to pynb markdown

# Example: Using Literate Script Content

This file demonstrates how to write literate F# script
files (`*.fsx`) that can be transformed into nice HTML
using the `literate.fsx` script from the [F# Formatting
package](http://fsprojects.github.io/FSharp.Formatting).

As you can see, a comment starting with double asterisk
is treated as part of the document and is transformed
using Markdown, which means that you can use:

// can't yet format Span ([Literal ("Unordered or ordered lists", Some { StartLine = 19 StartColumn = 3 EndLine = 19 EndColumn = 29 })], Some { StartLine = 19 StartColumn = 0 EndLine = 19 EndColumn = 30 }) to pynb markdown
// can't yet format Span ([Literal ("Text formatting including ", Some { StartLine = 20 StartColumn = 3 EndLine = 21 EndColumn = 29 }); Strong ([Literal ("bold", Some { StartLine = 20 StartColumn = 29 EndLine = 21 EndColumn = 33 })], Some { StartLine = 20 StartColumn = 29 EndLine = 21 EndColumn = -1 }); Literal (" and ", Some { StartLine = 20 StartColumn = 29 EndLine = 21 EndColumn = 34 }); Emphasis ([Literal ("emphasis", Some { StartLine = 20 StartColumn = 34 EndLine = 21 EndColumn = 42 })], Some { StartLine = 20 StartColumn = 34 EndLine = 21 EndColumn = -1 })], Some { StartLine = 19 StartColumn = 0 EndLine = 19 EndColumn = 30 }) to pynb markdown
And numerous other [Markdown](http://daringfireball.net/projects/markdown) features.

## Writing F# code

Code that is not inside comment will be formatted as
a sample snippet.

// can't yet format InlineHtmlBlock ("/// The Hello World of functional languages!
let rec factorial x = 
  if x = 0 then 1 
  else x * (factorial (x - 1))

let f10 = factorial 10", None, None) to pynb markdown

## Hiding code

If you want to include some code in the source code,
but omit it from the output, you can use the `hide`
command.

The value will be defined in the F# code and so you
can use it from other (visible) code and get correct
tool tips:

// can't yet format InlineHtmlBlock ("let answer = hidden", None, None) to pynb markdown

## Moving code around

Sometimes, it is useful to first explain some code that
has to be located at the end of the snippet (perhaps
because it uses some definitions discussed in the middle).
This can be done using `include` and `define` commands.

The following snippet gets correct tool tips, even though
it uses `laterFunction`:

// can't yet format InlineHtmlBlock ("let sample = 
  laterFunction()
  |> printfn "Got: %s"", None, None) to pynb markdown

Then we can explain how `laterFunction` is defined:

// can't yet format InlineHtmlBlock ("let laterFunction() = 
  "Not very difficult, is it?"", None, None) to pynb markdown

This example covers pretty much all features that are
currently implemented in `literate.fsx`, but feel free
to [fork the project on GitHub](https://github.com/fsprojects/FSharp.Formatting) and add more
features or report bugs!


