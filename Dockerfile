FROM fsharp:10.6.0-netcore AS build-env
WORKDIR /app

COPY .paket/ .paket/
COPY paket.lock ./
COPY paket.dependencies ./
COPY paket.references ./

RUN mono .paket/paket.bootstrapper.exe
RUN mono .paket/paket.exe restore --verbose

# Install Dotnet 3
#RUN apt-get update \
#  && apt install -y wget gpg \
#  && wget -qO- https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor > microsoft.asc.gpg \
#  && mv microsoft.asc.gpg /etc/apt/trusted.gpg.d/ \
#  && wget -q https://packages.microsoft.com/config/debian/9/prod.list \
#  && mv prod.list /etc/apt/sources.list.d/microsoft-prod.list \
#  && chown root:root /etc/apt/trusted.gpg.d/microsoft.asc.gpg \
#  && chown root:root /etc/apt/sources.list.d/microsoft-prod.list \
#  && apt-get update \
#  && apt-get install -y apt-transport-https \
#  && apt-get update \
#  && apt-get install -y dotnet-sdk-3.0 

COPY . ./
RUN dotnet publish -c Release -o out ./bekk-arrangement-svc.fsproj

CMD dotnet out/bekk-arrangement-svc.dll

#FROM mcr.microsoft.com/dotnet/core/runtime:3.0-buster-slim
#WORKDIR /app
#COPY --from=build-env /app/out .
#COPY --from=build-env /app/hahaha.sh .
#CMD dotnet bekk-arrangement-svc.dll
#CMD ./hahaha.sh
