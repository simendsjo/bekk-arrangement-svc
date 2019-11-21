FROM fsharp:10.2.3-netcore AS build-env
WORKDIR /app

COPY . ./

COPY .paket/ .paket/
COPY paket.lock ./
COPY paket.dependencies ./
COPY paket.references ./

RUN mono .paket/paket.bootstrapper.exe
RUN mono .paket/paket.exe install
RUN mono .paket/paket.exe restore

COPY . ./
RUN dotnet publish -c Release -o out ./bekk-arrangement-svc.fsproj
#ENV ASPNETCORE_URLS="http://0.0.0.0:80"
#CMD dotnet out/bekk-arrangement-svc.dll

#FROM mcr.microsoft.com/dotnet/core/runtime:3.0-buster-slim
#WORKDIR /app
#COPY --from=build-env /app/out .
#COPY --from=build-env /app/hahaha.sh .
#CMD dotnet bekk-arrangement-svc.dll
#CMD ./hahaha.sh

#FROM mcr.microsoft.com/dotnet/core/aspnet:3.0 AS runtime
#FROM mcr.microsoft.com/dotnet/core/aspnet:2.0
FROM microsoft/dotnet:2.2-aspnetcore-runtime
ENV ASPNETCORE_URLS=http://+:80
WORKDIR /app
COPY --from=build-env /app/out .
CMD dotnet bekk-arrangement-svc.dll