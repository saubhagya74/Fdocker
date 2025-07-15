# Step 1: Build the app
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
# working directory banako
WORKDIR /app
# paila package file import  hanya 
COPY *app.csproj ./
# package install hanya
RUN dotnet restore
# whole project copy hanya app ma 
COPY . ./
#copy garera production mode release hanya for dll creation
RUN dotnet publish -c Release -o backend
# Step 2 (Run): asp.net 9 user garya aaile chai
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
# working directory ma aako 
WORKDIR /app
# released app copy hanya app/backend ma
COPY --from=build /app/backend .
# finallyt run hanya
ENTRYPOINT ["dotnet", "app.dll"]
