# to make a single self-running exe
dotnet publish --no-self-contained -r win-x64 -c Release -v d --interactive -p:IncludeNativeLibrariesForSelfExtract=true