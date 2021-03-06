﻿open FSharp.Data

type Elements = JsonProvider<"""STU3\profiles-resources.json""">

let getPathTypes (filename: string) =

    let file = Elements.Load(filename)
    let snapshots = file.Entry |> Array.collect (fun e -> e.Resource.Snapshot |> Option.toArray)
    let elements = snapshots |> Array.collect (fun s -> s.Element)


    let pathTypes = seq {
        for element in elements do

            match element.ContentReference with
            | Some ref ->
                if not <| ref.StartsWith("#") then failwithf "ContentReference invalid for %s" element.Id
                yield element.Id, ref.Substring(1)

            | None ->
                let typeCodes =
                    element.Type
                    |> Array.map (fun t -> t.Code)
                    |> Array.distinct

                match typeCodes with
                | [| |] ->
                    if element.Id.Contains(".") then
                        yield element.Id, "()"
                | [| code |] ->
                    yield element.Id, code
                | multiple ->
                    if element.Id.EndsWith("[x]") = false then
                        failwithf "multiple types but no [x]: %s %A" element.Id multiple
            
                    let prefix = element.Id.Substring(0, element.Id.Length - 3)
                    for code in multiple do
                        let typeTitleCase = (string code.[0]).ToUpper() + code.Substring(1)
                        yield prefix + typeTitleCase, code
    }
    
    pathTypes

[<EntryPoint>]
let main argv =
    printfn "// -----------------------------------------"
    printfn "// Generated by FHIR PathsByType Utility"
    printfn "// -----------------------------------------"
    printfn ""
    printfn "package models2"
    printfn ""

    let types = Seq.concat [
                    getPathTypes """STU3\profiles-resources.json"""
                    getPathTypes """STU3\profiles-types.json"""
                ] |> Seq.sortBy fst

    printfn """var fhirTypes = map[string]string {"""
    for path, t in types do
        printfn """    "%s": "%s",""" path t
    printfn "}"

    0 // exit code
