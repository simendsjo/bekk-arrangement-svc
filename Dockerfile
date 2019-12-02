FROM fsharp:10.2.3-netcore AS build-env
WORKDIR /app

COPY . ./
RUN dotnet publish -c Release -o out ./src/bekk-arrangement-svc.fsproj

FROM microsoft/dotnet:2.2-aspnetcore-runtime
COPY --from=build-env /app/src/out .

ENV VIRTUAL_PATH="/arrangment-svc"
ENV PORT=80
CMD dotnet arrangementSvc.dll
