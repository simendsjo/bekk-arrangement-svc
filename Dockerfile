FROM fsharp:netcore AS build-env
WORKDIR /app

COPY . ./
RUN dotnet publish -c Release -o out ./src/bekk-arrangement-svc.fsproj

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-bionic
COPY --from=build-env /app/out .

ENV VIRTUAL_PATH="/arrangment-svc"
ENV PORT=80
CMD dotnet arrangementSvc.dll
