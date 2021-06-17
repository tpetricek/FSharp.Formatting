// can't yet format YamlFrontmatter (["title: Markdown LaTeX"; "category: Examples"; "categoryindex: 2"; "index: 3"], Some { StartLine = 1 StartColumn = 0 EndLine = 5 EndColumn = 8 }) to pynb markdown

# Example: Using the Markdown Extensions for LaTeX

To use LaTex extension, you need add javascript
link to [MathJax](http://www.mathjax.org/) in
your template.

To use inline LaTex, eclose LaTex code with `$`:
$k_{n+1} = n^2 + k_n^2 - k_{n-1}$. Alternatively,
you can also use `$$`.

To use block LaTex, start a new parapgraph, with
the first line marked as `$$$` (no close `$$$`):

\begin{equation}
A_{m,n} =
 \begin{pmatrix}
  a_{1,1} & a_{1,2} & \cdots & a_{1,n} \\
  a_{2,1} & a_{2,2} & \cdots & a_{2,n} \\
  \vdots  & \vdots  & \ddots & \vdots  \\
  a_{m,1} & a_{m,2} & \cdots & a_{m,n}
 \end{pmatrix}
\end{equation}

Use LaTex escape rule:

// can't yet format Span ([Literal ("Escape $ in inline mode: ", Some { StartLine = 32 StartColumn = 2 EndLine = 32 EndColumn = 27 }); LatexInlineMath ("\$", Some { StartLine = 32 StartColumn = 28 EndLine = 32 EndColumn = 30 }); Literal (", ", Some { StartLine = 32 StartColumn = 28 EndLine = 32 EndColumn = 30 }); LatexInlineMath ("\$var", Some { StartLine = 32 StartColumn = 31 EndLine = 32 EndColumn = 36 })], Some { StartLine = 32 StartColumn = 0 EndLine = 32 EndColumn = 45 }) to pynb markdown
// can't yet format Span ([Literal ("Other escapes: ", Some { StartLine = 33 StartColumn = 2 EndLine = 33 EndColumn = 17 }); LatexInlineMath ("\& \% \$ \# \_ \{ \}", Some { StartLine = 33 StartColumn = 18 EndLine = 33 EndColumn = 38 })], Some { StartLine = 32 StartColumn = 0 EndLine = 32 EndColumn = 45 }) to pynb markdown
// can't yet format Span ([Literal ("Using < or >: ", Some { StartLine = 34 StartColumn = 2 EndLine = 35 EndColumn = 16 }); LatexInlineMath ("x > 1", Some { StartLine = 34 StartColumn = 17 EndLine = 35 EndColumn = 22 }); Literal (", ", Some { StartLine = 34 StartColumn = 17 EndLine = 35 EndColumn = 19 }); LatexInlineMath ("y < 1", Some { StartLine = 34 StartColumn = 20 EndLine = 35 EndColumn = 25 }); Literal (", ", Some { StartLine = 34 StartColumn = 20 EndLine = 35 EndColumn = 22 }); LatexInlineMath ("x >= 1", Some { StartLine = 34 StartColumn = 23 EndLine = 35 EndColumn = 29 }); Literal (",
", Some { StartLine = 34 StartColumn = 23 EndLine = 35 EndColumn = 25 }); LatexInlineMath ("y <= 1", Some { StartLine = 34 StartColumn = 26 EndLine = 35 EndColumn = 32 }); Literal (", ", Some { StartLine = 34 StartColumn = 26 EndLine = 35 EndColumn = 28 }); LatexInlineMath ("x = 1", Some { StartLine = 34 StartColumn = 29 EndLine = 35 EndColumn = 34 })], Some { StartLine = 32 StartColumn = 0 EndLine = 32 EndColumn = 45 }) to pynb markdown
// can't yet format Span ([LatexInlineMath ("<p>something</p>", Some { StartLine = 36 StartColumn = 3 EndLine = 36 EndColumn = 19 })], Some { StartLine = 32 StartColumn = 0 EndLine = 32 EndColumn = 45 }) to pynb markdown

