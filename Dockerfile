FROM fsharp AS build-env
WORKDIR /app

COPY .paket/ .paket/
COPY paket.lock ./
COPY paket.dependencies ./
COPY paket.references ./

RUN mono .paket/paket.bootstrapper.exe
RUN mono .paket/paket.exe restore --verbose

# Install Dotnet 3
RUN apt-get update \
  && apt install -y wget gpg \
  && wget -qO- https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor > microsoft.asc.gpg \
  && mv microsoft.asc.gpg /etc/apt/trusted.gpg.d/ \
  && wget -q https://packages.microsoft.com/config/debian/9/prod.list \
  && mv prod.list /etc/apt/sources.list.d/microsoft-prod.list \
  && chown root:root /etc/apt/trusted.gpg.d/microsoft.asc.gpg \
  && chown root:root /etc/apt/sources.list.d/microsoft-prod.list \
  && apt-get update \
  && apt-get install -y apt-transport-https \
  && apt-get update \
  && apt-get install -y dotnet-sdk-3.0 

COPY . ./
RUN dotnet publish -c Release -o out ./bekk-arrangement-svc.fsproj

ENV ConnectionStrings__EventDb=Server=rds-dev.bekk.local;User=event-svc;Password=9U6&i5xvvBkG%Bs*S&eT;Database=event-svc
CMD dotnet out/bekk-arrangement-svc.dll

#FROM microsoft/dotnet:3.0-aspnetcore-runtime
#WORKDIR /app
#COPY --from=build-env /app/out .

#CMD dotnet bekk-arrangement-svc.dll
