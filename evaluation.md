// can't yet format YamlFrontmatter (["category: Documentation"; "categoryindex: 1"; "index: 6"], Some { StartLine = 2 StartColumn = 0 EndLine = 5 EndColumn = 8 }) to pynb markdown

[![Binder](img/badge-binder.svg)](https://mybinder.org/v2/gh/diffsharp/diffsharp.github.io/master?filepath=evaluation.ipynb)&emsp;
[![Script](img/badge-script.svg)](evaluation.fsx)&emsp;
[![Notebook](img/badge-notebook.svg)](evaluation.ipynb)

# Embedding script output

For literate F# scripts, you may embed the result of running the script as part of the literate output.
This is a feature of the functions discussed in [literate programming](literate.html) and
it is implemented using the [F# Compiler service](http://fsharp.github.io/FSharp.Compiler.Service/).

## Including Console Output

To include the Console output use `include-output`:

// can't yet format InlineHtmlBlock ("let test = 40 + 2

printf "A result is: %d" test
(*** include-output ***)
", None, None) to pynb markdown

The script defines a variable `test` and then prints it. The console output is included
in the output.

To include the a formatted value use `include-it`:

// can't yet format InlineHtmlBlock ("[ 0 .. 99 ]

(*** include-it ***)
", None, None) to pynb markdown

To include the meta output of F# Interactive processing such as type signatures use `(*** include-fsi-output ***)`:

// can't yet format InlineHtmlBlock ("let test = 40 + 3

(*** include-fsi-output ***)
", None, None) to pynb markdown

To include both console otuput and F# Interactive output blended use `(*** include-fsi-merged-output ***)`.

// can't yet format InlineHtmlBlock ("let test = 40 + 4
(*** include-fsi-merged-output ***)
", None, None) to pynb markdown

You can use the same commands with a named snippet:

// can't yet format InlineHtmlBlock ("(*** include-it: test ***)
(*** include-fsi-output: test ***)
(*** include-output: test ***)
", None, None) to pynb markdown

You can use the `include-value` command to format a specific value:

// can't yet format InlineHtmlBlock ("let value1 = [ 0 .. 50 ]
let value2 = [ 51 .. 100 ]
(*** include-value: value1 ***)
", None, None) to pynb markdown

## Using AddPrinter and AddHtmlPrinter

You can use `fsi.AddPrinter`, `fsi.AddPrintTransformer` and `fsi.AddHtmlPrinter` to extend the formatting of objects.

## Emitting Raw Text

To emit raw text in F# literate scripts use the following:

// can't yet format InlineHtmlBlock ("(**
	(*** raw ***)
	Some raw text.
*)
", None, None) to pynb markdown

which would emit

// can't yet format InlineHtmlBlock ("<pre>
Some raw text.
</pre>", None, Some { StartLine = 70 StartColumn = 0 EndLine = 70 EndColumn = 5 }) to pynb markdown

directly into the document.

## F# Formatting as a Library:  Specifying the Evaluator and Formatting

If using F# Formatting as a library the embedding of F# output requires specifying an additional parameter to the
parsing functions discussed in [literate programming documentation](literate.html).
Assuming you have all the references in place, you can now create an instance of
[FsiEvaluator](https://fsprojects.github.io/FSharp.Formatting/reference/fsharp-formatting-literate-evaluation-fsievaluator.html) that represents a wrapper for F# interactive and pass it to all the
functions that parse script files or process script files:

// can't yet format InlineHtmlBlock ("open FSharp.Formatting.Literate
open FSharp.Formatting.Literate.Evaluation
open FSharp.Formatting.Markdown

// Sample literate content
let content = """
let a = 10
(*** include-value:a ***)"""

// Create evaluator and parse script
let fsi = FsiEvaluator()
let doc = Literate.ParseScriptString(content, fsiEvaluator = fsi)
Literate.ToHtml(doc)", None, None) to pynb markdown

When the `fsiEvaluator` parameter is specified, the script is evaluated and so you
can use additional commands such as `include-value`. When the evaluator is **not** specified,
it is not created automatically and so the functionality is not available (this way,
you won't accidentally run unexpected code!)

If you specify the `fsiEvaluator` parameter, but don't want a specific snippet to be evaluated
(because it might throw an exception, for example), you can use the `(*** do-not-eval ***)`
command.

The constructor of [FsiEvaluator](https://fsprojects.github.io/FSharp.Formatting/reference/fsharp-formatting-literate-evaluation-fsievaluator.html) takes command line parameters for `fsi.exe` that can
be used to specify, for example, defined symbols and other attributes for F# Interactive.

You can also subscribe to the `EvaluationFailed` event which is fired whenever the evaluation
of an expression fails. You can use that to do tests that verify that all off the code in your
documentation executes without errors.

## F# Formatting as a Library: Custom formatting functions

As mentioned earlier, values are formatted using a simple `"%A"` formatter by default.
However, you can specify a formatting function that provides a nicer formatting for values
of certain types. For example, let's say that we would want to format F# lists such as
`[1; 2; 3]` as HTML ordered lists `<ol>`.

This can be done by calling [FsiEvaluator.RegisterTransformation](https://fsprojects.github.io/FSharp.Formatting/reference/fsharp-formatting-literate-evaluation-fsievaluator.html) on the `FsiEvaluator` instance:

// can't yet format InlineHtmlBlock ("// Create evaluator & register simple formatter for lists
let fsiOl = FsiEvaluator()
fsiOl.RegisterTransformation(fun (o, ty, _executionCount) ->
  // If the type of value is an F# list, format it nicely
  if ty.IsGenericType && ty.GetGenericTypeDefinition() = typedefof<list<_>> then
    let items = 
      // Get items as objects and create paragraph for each item
      [ for it in Seq.cast<obj> (unbox o) -> 
          [ Paragraph([Literal(it.ToString(), None)], None) ] ]
    // Return option value (success) with ordered list
    Some [ ListBlock(MarkdownListKind.Ordered, items, None) ]
  else None)", None, None) to pynb markdown

The function is called with two arguments - `o` is the value to be formatted and `ty`
is the static type of the value (as inferred by the F# compiler). The sample checks
that the type of the value is a list (containing values of any type) and then it
casts all values in the list to `obj` (for simplicity). Then we generate Markdown
blocks representing an ordered list. This means that the code will work for both
LaTeX and HTML formatting - but if you only need one, you can simply produce HTML and
embed it in `InlineHtmlBlock`.

To use the new `FsiEvaluator`, we can use the same style as earlier. This time, we format
a simple list containing strings:

// can't yet format InlineHtmlBlock ("let listy = """
### Formatting demo
let test = ["one";"two";"three"]
(*** include-value:test ***)"""

let docOl = Literate.ParseScriptString(listy, fsiEvaluator = fsiOl)
Literate.ToHtml(docOl)", None, None) to pynb markdown

The resulting HTML formatting of the document contains the snippet that defines `test`,
followed by a nicely formatted ordered list:

// can't yet format InlineHtmlBlock ("<blockquote>
<h3>Formatting demo</h3>
<table class="pre"><tr><td class="lines"><pre class="fssnip">
<span class="l">1: </span>
</pre>
</td>
<td class="snippet"><pre class="fssnip">
<span class="k">let</span> <spanclass="i">test</span> <span class="o">=</span> [<span class="s">&quot;</span><span class="s">one</span><span class="s">&quot;</span>;<span class="s">&quot;</span><span class="s">two</span><span class="s">&quot;</span>;<span class="s">&quot;</span><span class="s">three</span><span class="s">&quot;</span>]</pre>
</td>
</tr>
</table>
<ol>
<li><p>one</p></li>
<li><p>two</p></li>
<li><p>three</p></li>
</ol>
</blockquote>", None, Some { StartLine = 5 StartColumn = 0 EndLine = 5 EndColumn = 12 }) to pynb markdown


