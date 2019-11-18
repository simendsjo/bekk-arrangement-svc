FROM fsharp:10.2.3-netcore AS build-env
WORKDIR /app

COPY src/ .
RUN dotnet publish -c Release -o out

FROM microsoft/dotnet:2.2-aspnetcore-runtime

FROM microsoft/dotnet:3.0-aspnetcore-runtime
COPY --from=build-env /app/out .

ENV VIRTUAL_PATH="/arrangment-svc"
ENV PORT=80
CMD dotnet arrangementSvc.dll