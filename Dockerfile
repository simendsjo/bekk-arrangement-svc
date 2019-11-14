FROM fsharp:10.6.0-netcore AS build-env
WORKDIR /app

COPY .paket/ .paket/
COPY paket.lock ./
COPY paket.dependencies ./
COPY paket.references ./

RUN mono .paket/paket.bootstrapper.exe
RUN mono .paket/paket.exe install
RUN mono .paket/paket.exe restore

COPY src/ src/
RUN dotnet publish src/bekk-arrangement-svc.fsproj -c Release -o out

FROM microsoft/dotnet:3.0-aspnetcore-runtime

COPY --from=build-env /app/out .

ENV VIRTUAL_PATH="/arrangment-svc"
ENV PORT=80
CMD dotnet arrangementSvc.dll