F# Formatting ![Build and Test](https://github.com/fsprojects/FSharp.Formatting/actions/workflows/push-master.yml/badge.svg)
=================================

The FSharp.Formatting package includes libraries and tools for processing F# script files, markdown and components
for documentation generation.

See https://fsprojects.github.io/FSharp.Formatting/


## Development

    .\build.cmd
    ./build.sh


One built you can run the command-line tool to self-build the docs for this directory using 

    dotnet build
    src\FSharp.Formatting.CommandTool\bin\Debug\net5.0\fsdocs.exe watch
    src\FSharp.Formatting.CommandTool\bin\Debug\net5.0\fsdocs.exe build --clean


## Maintainer(s)

- [@dsyme](https://github.com/dsyme)
- [@eiriktsarpalis](https://github.com/eiriktsarpalis)
