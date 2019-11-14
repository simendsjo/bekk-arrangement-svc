FROM fsharp:10.2.1-netcore AS build-env
WORKDIR /app

# COPY . ./

# COPY .paket/ .paket/
# COPY paket.lock ./
# COPY paket.dependencies ./
# COPY paket.references ./

# RUN mono .paket/paket.bootstrapper.exe
# RUN mono .paket/paket.exe install
# RUN mono .paket/paket.exe restore

# COPY . ./
COPY src/app .
RUN dotnet publish -c Release -o out
#ENV ASPNETCORE_URLS="http://0.0.0.0:80"
#CMD dotnet out/bekk-arrangement-svc.dll

#FROM mcr.microsoft.com/dotnet/core/runtime:3.0-buster-slim
#WORKDIR /app
#COPY --from=build-env /app/out .
#COPY --from=build-env /app/hahaha.sh .
#CMD dotnet bekk-arrangement-svc.dll
#CMD ./hahaha.sh

#FROM mcr.microsoft.com/dotnet/core/aspnet:3.0 AS runtime
# FROM mcr.microsoft.com/dotnet/core/aspnet:3.0
# ENV ASPNETCORE_URLS=http://+:80
# WORKDIR /app
# COPY --from=build-env /app/out .
# CMD dotnet bekk-arrangement-svc.dll


FROM microsoft/dotnet:2.1-aspnetcore-runtime

COPY --from=build-env /app/out .

ENV VIRTUAL_PATH="/arrangment-svc"
ENV PORT=80
CMD dotnet arrangementSvc.dll
