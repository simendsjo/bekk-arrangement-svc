FROM mcr.microsoft.com/dotnet/sdk:5.0-alpine as build-env
WORKDIR /app

COPY . ./
RUN dotnet publish -c Release -o out ./src/bekk-arrangement-svc.fsproj

FROM mcr.microsoft.com/dotnet/sdk:5.0-alpine
COPY --from=build-env /app/out .

ENV VIRTUAL_PATH="/arrangement-svc"
ENV PORT=80
CMD dotnet arrangementSvc.dll
