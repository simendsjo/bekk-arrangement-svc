FROM fsharp:10.6.0-netcore AS build-env
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
ENV PORT=80
CMD dotnet out/bekk-arrangement-svc.dll

#FROM mcr.microsoft.com/dotnet/core/runtime:3.0-buster-slim
#WORKDIR /app
#COPY --from=build-env /app/out .
#COPY --from=build-env /app/hahaha.sh .
#CMD dotnet bekk-arrangement-svc.dll
#CMD ./hahaha.sh
