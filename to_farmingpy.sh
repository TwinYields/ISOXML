# Build libary and copy to farmingpy
dotnet build --configuration Release
cp ISOXML/bin/Release/netstandard2.1/ISOXML.dll ../farmingpy/farmingpy/data