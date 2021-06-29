module internal FSharp.Formatting.ApiDocs.GenerateHtml

open System
open System.Collections.Generic
open System.IO
open System.Web
open FSharp.Formatting.Common
open FSharp.Compiler.Symbols
open FSharp.Formatting.Templating
open FSharp.Formatting.HtmlModel
open FSharp.Formatting.HtmlModel.Html

/// Embed some HTML generateed in GenerateModel
let embed (x: ApiDocHtml) = !! x.HtmlText

type HtmlRender(model: ApiDocModel) =
  let root = model.Root
  let collectionName = model.Collection.CollectionName
  let qualify = model.Qualify

  //let obsoleteMessage msg =
  //  div [Class "alert alert-warning"] [
  //      strong [] [!!"NOTE:"]
  //      p [] [!! ("This API is obsolete" + HttpUtility.HtmlEncode(msg))]
  //  ]

  let mutable uniqueNumber = 0
  let UniqueID() =
    uniqueNumber <- uniqueNumber + 1
    uniqueNumber

  let codeWithToolTip content tip =
    div [] [
      let id = UniqueID().ToString()
      code [ OnMouseOut  (sprintf "hideTip(event, '%s', %s)" id id)
             OnMouseOver (sprintf "showTip(event, '%s', %s)" id id)] content
      div [Class "fsdocs-tip"; Id id ] tip
    ]

  let sourceLink url =
      [ match url with
        | None -> ()
        | Some href ->
          a [Href href; Class"fsdocs-source-link"; HtmlProperties.Title "Source on GitHub" ] [
            img [Src (sprintf "%scontent/img/github.png" root); Class "normal"]
            img [Src (sprintf "%scontent/img/github-hover.png" root); Class "hover"]
          ] ]

  let removeParen (memberName:string) =
    let firstParen = memberName.IndexOf("(")
    if firstParen > 0 then memberName.Substring(0, firstParen) else memberName

  // Copy XML sig for use in `cref` XML
  let copyXmlSigIcon xmlDocSig =
      div [ Class"fsdocs-source-link"; HtmlProperties.Title "Copy signature (XML)"; OnClick (sprintf "Clipboard_CopyTo('<see cref=\\\'%s\\\'/>')" xmlDocSig) ] [
            img [Src (sprintf "%scontent/img/copy-xml.png" root); Class "normal"]
            img [Src (sprintf "%scontent/img/copy-xml-hover.png" root); Class "hover"]
          ]

  let copyXmlSigIconForSymbol (symbol: FSharpSymbol) =
    [ match symbol with
      | :? FSharpMemberOrFunctionOrValue as v ->
        copyXmlSigIcon (removeParen v.XmlDocSig)
      | :? FSharpEntity as v ->
        copyXmlSigIcon (removeParen v.XmlDocSig)
      | _ ->
        ()
         ]

  // Copy XML sig for use in `cref` markdown
  let copyXmlSigIconMarkdown (xmlDocSig: string) =
    if xmlDocSig.StartsWith("`") || xmlDocSig.EndsWith("`")  then div [ ] []
    else
      let delim =
          if xmlDocSig.Contains("``") then "```"
          elif xmlDocSig.Contains("`") then "``"
          else "`"
      div [ Class"fsdocs-source-link"; HtmlProperties.Title "Copy signature (Markdown)"; OnClick (sprintf "Clipboard_CopyTo('%scref:%s%s')" delim xmlDocSig delim) ] [
            img [Src (sprintf "%scontent/img/copy-md.png" root); Class "normal"]
            img [Src (sprintf "%scontent/img/copy-md-hover.png" root); Class "hover"]
          ]

  let copyXmlSigIconForSymbolMarkdown (symbol: FSharpSymbol) =
    [ match symbol with
      | :? FSharpMemberOrFunctionOrValue as v ->
        copyXmlSigIconMarkdown (removeParen v.XmlDocSig)
      | :? FSharpEntity as v ->
        copyXmlSigIconMarkdown (removeParen v.XmlDocSig)
      | _ ->
        ()
         ]

  let renderMembers header tableHeader (members: ApiDocMember list) =
   [ if members.Length > 0 then
       h3 [] [!! header]
       table [Class "table outer-list fsdocs-member-list"] [
         thead [] [
           tr [] [
             td [Class "fsdocs-member-list-header"] [ !!tableHeader ]
             td [Class "fsdocs-member-list-header"] [ !! "Description" ]
           ]
         ]
         tbody [] [
           for m in members do
             tr [] [
               td [Class "fsdocs-member-usage"] [

                  codeWithToolTip [
                      // This adds #MemberName anchor. These may be ambiguous due to overloading
                      p [] [a [Id m.Name] [a [Href ("#"+m.Name)] [embed m.UsageHtml]]]
                    ]
                    [
                      div [Class "member-tooltip"] [
                        !! "Full Usage: "
                        embed m.UsageHtml
                        br []
                        br []
                        if not m.Parameters.IsEmpty then
                            !! "Parameters: "
                            ul [] [
                              for p in m.Parameters do
                                span [] [
                                    b [] [!! p.ParameterNameText ];
                                    !! ":"; embed p.ParameterType
                                    match p.ParameterDocs with
                                    | None -> ()
                                    | Some d -> !! " - "; embed d]
                                br []
                            ]
                            br []
                        match m.ReturnInfo.ReturnType with
                        | None -> ()
                        | Some rty ->
                            span [] [!! (if m.Kind <> ApiDocMemberKind.RecordField then "Returns: " else "Field type: "); embed rty ]
                            match m.ReturnInfo.ReturnDocs with
                            | None -> ()
                            | Some d -> embed d
                            br []
                        //!! "Signature: "
                        //encode(m.SignatureTooltip)
                        if not m.Modifiers.IsEmpty then
                          !! "Modifiers: "
                          encode(m.FormatModifiers)
                          br []

                          // We suppress the display of ill-formatted type parameters for places
                          // where these have not been explicitly declared
                          match m.FormatTypeArguments with
                          | None -> ()
                          | Some v ->
                              !!"Type parameters: "
                              encode(v)
                      ]
                    ]
               ]

               td [Class "fsdocs-member-xmldoc"] [
                  div [Class "fsdocs-summary"]
                     [ yield! copyXmlSigIconForSymbolMarkdown m.Symbol
                       yield! copyXmlSigIconForSymbol m.Symbol
                       yield! sourceLink m.SourceLocation
                       p [Class "fsdocs-summary"] [ embed m.Comment.Summary ]; ]

                  match m.Comment.Remarks with
                  | Some r ->
                      p [Class "fsdocs-remarks"] [embed r]
                  | None -> ()

                  match m.ExtendedType with
                  | Some s ->
                      p [] [!! "Extended Type: "; embed s ]
                  | _ -> ()

                  if not m.Parameters.IsEmpty then
                      dl [Class "fsdocs-params"] [
                          for parameter in m.Parameters do
                              dt [Class "fsdocs-param"] [
                                  span [Class "fsdocs-param-name"] [!! parameter.ParameterNameText]
                                  !! ":"
                                  embed parameter.ParameterType
                              ]
                              dd [Class "fsdocs-param-docs"] [
                                  match parameter.ParameterDocs with
                                  | None -> ()
                                  | Some d -> p [] [embed d]
                              ]
                      ]

                  match m.ReturnInfo.ReturnType with
                  | None -> ()
                  | Some t ->
                      dl [Class "fsdocs-returns"] [
                          dt [] [
                              span [Class "fsdocs-return-name"] [!! (if m.Kind <> ApiDocMemberKind.RecordField then "Returns: " else "Field type: ")]
                              embed t
                          ]
                          dd [Class "fsdocs-return-docs"] [
                              match m.ReturnInfo.ReturnDocs with
                              | None -> ()
                              | Some r -> p [] [embed r]
                          ]
                      ]

                  if not m.Comment.Exceptions.IsEmpty then
                      //p [] [ !! "Exceptions:" ]
                      table [Class "fsdocs-exception-list"] [
                          for (nm, link, html) in m.Comment.Exceptions do
                            tr [] [td [] (match link with None -> [] | Some href -> [a [Href href] [!! nm] ])
                                   td [] [embed html]]
                      ]

                  for e in m.Comment.Notes do
                      h5 [Class "fsdocs-note-header"] [!! "Note"]
                      p [Class "fsdocs-note"] [embed e]

                  for e in m.Comment.Examples do
                      h5 [Class "fsdocs-example-header"] [!! "Example"]
                      p [Class "fsdocs-example"] [embed e]

                  //if m.IsObsolete then
                  //    obsoleteMessage m.ObsoleteMessage

                  //if not (String.IsNullOrEmpty(m.Details.FormatCompiledName)) then
                  //    p [] [!!"CompiledName: "; code [] [!!m.Details.FormatCompiledName]]
               ]
            ]
          ]
        ]
    ]

  let renderEntities (entities: ApiDocEntity list) =
   [ if entities.Length > 0 then
      let hasTypes = entities |> List.exists (fun e -> e.IsTypeDefinition)
      let hasModules = entities |> List.exists (fun e -> not e.IsTypeDefinition)
      table [Class "table outer-list fsdocs-entity-list" ] [
        thead [] [
          tr [] [
            td [] [!! (if hasTypes && hasModules then "Type/Module" elif hasTypes then "Type" else "Modules")]
            td [] [!!"Description"]
          ]
        ]
        tbody [] [
          for e in entities do
            tr [] [
               td [Class "fsdocs-entity-name"] [
                 let nm = e.Name
                 let multi = (entities |> List.filter (fun e -> e.Name = nm) |> List.length) > 1
                 let nmWithSiffix = if multi then (if e.IsTypeDefinition then nm + " (Type)" else nm + " (Module)") else nm

                 // This adds #EntityName anchor. These may currently be ambiguous
                 p [] [a [Name nm] [a [Href (e.Url(root, collectionName, qualify, model.FileExtensions.InUrl))] [!!nmWithSiffix]]]
               ]
               td [Class "fsdocs-entity-xmldoc" ] [
                   div [] [
                       yield! copyXmlSigIconForSymbolMarkdown e.Symbol
                       yield! copyXmlSigIconForSymbol e.Symbol
                       yield! sourceLink e.SourceLocation
                       p [Class "fsdocs-summary" ] [ embed e.Comment.Summary ];
                   ]
               ]
            ]
        ]
      ]
   ]

  let entityContent (info: ApiDocEntityInfo) =
    // Get all the members & comment for the type
    let entity = info.Entity
    let members = entity.AllMembers |> List.filter (fun e -> not e.IsObsolete)
    let byCategory = members |> Categorise.getMembersByCategory

    let usageName =
        match info.ParentModule with
        | Some m when m.RequiresQualifiedAccess -> m.Name + "." + entity.Name
        | _ -> entity.Name

    [ h2 [] [!! (usageName + (if entity.IsTypeDefinition then " Type" else " Module")) ]
      dl [Class "fsdocs-metadata"] [
         dt [] [!! "Namespace: "; a [Href (info.Namespace.Url(root, collectionName, qualify, model.FileExtensions.InUrl))] [!!info.Namespace .Name]]
         dt [] [!! ("Assembly: " + entity.Assembly.Name + ".dll")]

         match info.ParentModule with
         | None -> ()
         | Some parentModule ->
           dt [] [!! "Parent Module: "; a [Href (parentModule.Url(root, collectionName, qualify, model.FileExtensions.InUrl))] [!! parentModule.Name ]]


         match entity.AbbreviatedType with
         | Some abbreviatedTyp ->
            dt [] [!! "Abbreviation For: "; embed abbreviatedTyp]

         | None ->  ()

         match entity.BaseType with
         | Some baseType ->
            dt [] [!! "Base Type: "; embed baseType]
         | None -> ()

         match entity.AllInterfaces with
         | [] -> ()
         | l ->
            dt [] [
               !! ("All Interfaces: ")
               for (i, ity) in Seq.indexed l do
                  if i <> 0 then
                     !! ", "
                  embed ity ]

         if entity.Symbol.IsValueType then
            dt [] [!! ("Kind: Struct")]

         match entity.DelegateSignature with
         | Some d ->
             dt [] [!! ("Delegate Signature: "); embed d]
         | None -> ()

         if entity.Symbol.IsProvided then
            dt [] [!! ("This is a provided type definition")]

         if entity.Symbol.IsAttributeType then
            dt [] [!! ("This is an attribute type definition")]

         if entity.Symbol.IsEnum then
            dt [] [!! ("This is an enum type definition")]

      //if info.Entity.IsObsolete then
      //    obsoleteMessage entity.ObsoleteMessage
      ]
      // Show the summary (and sectioned docs without any members)
      div [Class "fsdocs-xmldoc"] [
          div [] [
              //yield! copyXmlSigIconForSymbol entity.Symbol
              //yield! sourceLink entity.SourceLocation
              p [Class "fsdocs-summary" ] [ embed entity.Comment.Summary ];
          ]
          // Show the remarks etc.
          match entity.Comment.Remarks with
          | Some r ->
              p [Class "fsdocs-remarks"] [embed r]
          | None -> ()
          for note in entity.Comment.Notes do
              h5 [Class "fsdocs-note-header"] [!! "Note"]
              p [Class "fsdocs-note"] [embed note]

          for example in entity.Comment.Examples do
              h5 [Class "fsdocs-example-header"] [!! "Example"]
              p [Class "fsdocs-example"] [embed example]

      ]

      if (byCategory.Length > 1) then
        // If there is more than 1 category in the type, generate TOC
        h3 [] [!!"Table of contents"]
        ul [] [
          for (index, _, name) in byCategory do
            li [] [ a [Href (sprintf "#section%d" index)] [!! name ] ]
        ]

      //<!-- Render nested types and modules, if there are any -->

      let nestedEntities =
         entity.NestedEntities
         |> List.filter (fun e -> not e.IsObsolete)

      if (nestedEntities.Length > 0) then
        div [] [
          h3 [] [!!  (if nestedEntities |> List.forall (fun e -> not e.IsTypeDefinition)  then "Nested modules"
                      elif nestedEntities |> List.forall (fun e -> e.IsTypeDefinition) then "Types"
                      else "Types and nested modules")]
          yield! renderEntities nestedEntities
        ]

      for (index, ms, name) in byCategory do
        // Iterate over all the categories and print members. If there are more than one
        // categories, print the category heading (as <h2>) and add XML comment from the type
        // that is related to this specific category.
        if (byCategory.Length > 1) then
           h2 [Id (sprintf "section%d" index)] [!! name]
           //<a name="@(" section" + g.Index.ToString())">&#160;</a></h2>
        div [] (renderMembers "Functions and values" "Function or value" (ms |> List.filter (fun m -> m.Kind = ApiDocMemberKind.ValueOrFunction)))
        div [] (renderMembers "Type extensions" "Type extension" (ms |> List.filter (fun m -> m.Kind = ApiDocMemberKind.TypeExtension)))
        div [] (renderMembers "Active patterns" "Active pattern" (ms |> List.filter (fun m -> m.Kind = ApiDocMemberKind.ActivePattern)))
        div [] (renderMembers "Union cases" "Union case" (ms |> List.filter (fun m -> m.Kind = ApiDocMemberKind.UnionCase)))
        div [] (renderMembers "Record fields" "Record Field" (ms |> List.filter (fun m -> m.Kind = ApiDocMemberKind.RecordField)))
        div [] (renderMembers "Static parameters" "Static parameters" (ms |> List.filter (fun m -> m.Kind = ApiDocMemberKind.StaticParameter)))
        div [] (renderMembers "Constructors" "Constructor" (ms |> List.filter (fun m -> m.Kind = ApiDocMemberKind.Constructor)))
        div [] (renderMembers "Instance members" "Instance member" (ms |> List.filter (fun m -> m.Kind = ApiDocMemberKind.InstanceMember)))
        div [] (renderMembers "Static members" "Static member" (ms |> List.filter (fun m -> m.Kind = ApiDocMemberKind.StaticMember)))
    ]

  let namespaceContent (nsIndex, ns: ApiDocNamespace) =
    let allByCategory = Categorise.entities (nsIndex, ns, false)
    [ if allByCategory.Length > 0 then
        h2 [Id ns.UrlHash] [!! (ns.Name + " Namespace") ]

        div [Class "fsdocs-xmldoc"] [
            match ns.NamespaceDocs with
            | Some nsdocs ->
                p [] [embed nsdocs.Summary ]
                match nsdocs.Remarks with
                | Some r -> p [] [embed r ]
                | None -> ()

            | None -> ()
        ]

        if (allByCategory.Length > 1) then
            h3 [] [!! "Contents" ]

            ul [] [
               for category in allByCategory do
                   li [] [a [Href ("#category-" + category.CategoryIndex)] [!!category.CategoryName]]
            ]

        for category in allByCategory do
          if (allByCategory.Length > 1) then
             h3 [] [a [Class "anchor"; Name ("category-" + category.CategoryIndex); Href ("#category-" + category.CategoryIndex)] [!! category.CategoryName]]
          yield! renderEntities category.CategoryEntites
    ]

  let tableOfNamespacesAux () =
    [
      let categorise = Categorise.model model

      for _allByCategory, ns in categorise do

          // Generate the entry for the namespace
          tr [ ] [
              td [] [ a [ Href (ns.Url(root, collectionName, qualify, model.FileExtensions.InUrl))] [!!ns.Name] ]
              td [] [
                  match ns.NamespaceDocs with
                  | Some nsdocs -> embed nsdocs.Summary
                  | None -> ()
              ]
          ]
    ]

  let listOfNamespacesNavAux otherDocs (nsOpt: ApiDocNamespace option) =
    [
        // For FSharp.Core we make all entries available to other docs else there's not a lot else to show.
        //
        // For non-FSharp.Core we only show one link "API Reference" in the nav menu
      if otherDocs && model.Collection.CollectionName <> "FSharp.Core" then
          li [Class "nav-header"] [!! "API Reference"]
          li [ Class "nav-item"  ] [a [Class "nav-link"; Href (model.IndexFileUrl(root, collectionName, qualify, model.FileExtensions.InUrl))] [!! "All Namespaces" ] ]
      else

      let categorise = Categorise.model model

      let someExist = categorise.Length > 0

      if someExist then
        li [Class "nav-header"] [!! "Namespaces"]

      for allByCategory, ns in categorise do

          // Generate the entry for the namespace
          li [ Class ("nav-item" +
                          // add the 'active' class if this is the namespace of the thing being shown
                          match nsOpt with
                          | Some ns2 when ns.Name = ns2.Name -> " active"
                          | _ -> "") ] [
             span [] [
                    a [ Class ("nav-link" +
                             // add the 'active' class if this is the namespace of the thing being shown
                             match nsOpt with
                             | Some ns2 when ns.Name = ns2.Name -> " active"
                             | _ -> "")
                        Href (ns.Url(root, collectionName, qualify, model.FileExtensions.InUrl))] [!!ns.Name]

              ]
          ]

          // In the navigation bar generate the expanded list of entities
          // for the active namespace
          match nsOpt with
          | Some ns2 when ns.Name = ns2.Name ->
                  ul [ Custom ("list-style-type", "none") (* Class "navbar-nav " *) ] [
                      for category in allByCategory do
                          for e in category.CategoryEntites do
                              li [ Class "nav-item"  ] [a [Class "nav-link"; Href (e.Url(root, collectionName, qualify, model.FileExtensions.InUrl))] [!! e.Name] ]
                  ]
          | _ -> ()
     ]

  let listOfNamespacesNav otherDocs (nsOpt: ApiDocNamespace option) =
     listOfNamespacesNavAux otherDocs nsOpt
     |> List.map (fun html -> html.ToString()) |> String.concat "             \n"

  /// Get the substitutions relevant to all
  member _.GlobalSubstitutions : Substitutions =
    let toc = listOfNamespacesNav true None
    [ yield (ParamKeys.``fsdocs-list-of-namespaces``, toc )  ]

  member _.Generate(outDir: string, templateOpt, collectionName, globalParameters) =

    let getSubstitutons parameters toc (content: HtmlElement) pageTitle =
        [| yield! parameters
           yield (ParamKeys.``fsdocs-list-of-namespaces``, toc )
           yield (ParamKeys.``fsdocs-content``, content.ToString() )
           yield (ParamKeys.``fsdocs-source``, "" )
           yield (ParamKeys.``fsdocs-tooltips``, "" )
           yield (ParamKeys.``fsdocs-page-title``, pageTitle )
           yield! globalParameters
           |]

    let collection = model.Collection
    begin
        let content =
           div [] [
               h1 [] [!! "API Reference"];
               h2 [] [!! "Available Namespaces:"];
               table [Class "table outer-list fsdocs-member-list"] [
                 thead [] [
                   tr [] [
                      td [Class "fsdocs-member-list-header"] [ !! "Namespace" ]
                      td [Class "fsdocs-member-list-header"] [ !! "Description" ]
                   ]
                  ]
                 tbody [] (tableOfNamespacesAux())
               ]
           ]
        let pageTitle = sprintf "%s (API Reference)" collectionName
        let toc = listOfNamespacesNav false None
        let substitutions = getSubstitutons model.Substitutions toc content pageTitle
        let outFile = Path.Combine(outDir, model.IndexOutputFile(collectionName, model.Qualify, model.FileExtensions.InFile) )
        printfn "  Generating %s" outFile
        SimpleTemplating.UseFileAsSimpleTemplate (substitutions, templateOpt, outFile)
    end

    //printfn "Namespaces = %A" [ for ns in collection.Namespaces -> ns.Name ]

    for (nsIndex, ns) in Seq.indexed collection.Namespaces do
        let content = div [] (namespaceContent (nsIndex, ns))
        let pageTitle = ns.Name
        let toc = listOfNamespacesNav false (Some ns)
        let substitutions = getSubstitutons model.Substitutions toc content pageTitle
        let outFile = Path.Combine(outDir, ns.OutputFile(collectionName, model.Qualify, model.FileExtensions.InFile) )
        printfn "  Generating %s" outFile
        SimpleTemplating.UseFileAsSimpleTemplate (substitutions, templateOpt, outFile)

    for info in model.EntityInfos do
        let content = div [] (entityContent info)
        let pageTitle = sprintf "%s (%s)" info.Entity.Name collectionName
        let toc = listOfNamespacesNav false (Some info.Namespace)
        let substitutions = getSubstitutons info.Entity.Substitutions toc content pageTitle
        let outFile = Path.Combine(outDir, info.Entity.OutputFile(collectionName, model.Qualify, model.FileExtensions.InFile))
        printfn "  Generating %s" outFile
        SimpleTemplating.UseFileAsSimpleTemplate (substitutions, templateOpt, outFile)
