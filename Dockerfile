FROM mcr.microsoft.com/dotnet/core/sdk:3.0-alpine3.9 AS build-env
WORKDIR /app

RUN apk add bash && \
    apk add --no-cache mono \
       --repository http://dl-cdn.alpinelinux.org/alpine/edge/testing && \
    apk add --no-cache yarn \
       --repository http://dl-cdn.alpinelinux.org/alpine/edge/testing && \
    apk add --no-cache --virtual=.build-dependencies ca-certificates && \
    cert-sync /etc/ssl/certs/ca-certificates.crt && \
    apk del .build-dependencies
COPY .paket/ .paket/
COPY paket.dependencies ./
COPY paket.references ./

RUN wget https://github.com/fsprojects/Paket/releases/download/5.195.7/paket.exe \ 
    && chmod a+r paket.exe && mv paket.exe /usr/local/lib/ \ 
    && printf '#!/bin/sh\nexec /usr/bin/mono /usr/local/lib/paket.exe "$@"' >> /usr/local/bin/paket \ 
    && chmod u+x /usr/local/bin/paket

RUN paket install
RUN paket restore

# Copy csproj and restore as distinct layers
#COPY ./*.fsproj ./
##COPY paket.dependencies ./
#COPY paket.references ./

#RUN wget https://github.com/fsprojects/Paket/releases/download/5.195.7/paket.exe \ 
#    && chmod a+r paket.exe && mv paket.exe /usr/local/lib/ \ 
##    && printf '#!/bin/sh\nexec /usr/bin/mono /usr/local/lib/paket.exe "$@"' >> /usr/local/bin/paket \ 
  #  && chmod u+x /usr/local/bin/paket

##RUN apt install gnupg ca-certificates \
  #  && apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF \
   # && echo "deb https://download.mono-project.com/repo/ubuntu stable-bionic main" | tee /etc/apt/sources.list.d/mono-official-stable.list \
    #&& apt update
    
#RUN apt get install mono-devel

#RUN paket install
#RUN paket restore

# Copy everything else and build
#COPY . ./
#RUN dotnet publish -c Release -o out ./bekk-arrangement-svc.fsproj

# Build runtime image
#FROM microsoft/dotnet:3.0
#FROM microsoft/dotnet:3.0-aspnetcore-runtime
#FROM microsoft/dotnet:2.2-aspnetcore-runtime

#COPY --from=build-env /app/out .

#ENV VIRTUAL_PATH="/cabin-svc"
#ENV PORT=80

#CMD dotnet cabinSvc.dll
