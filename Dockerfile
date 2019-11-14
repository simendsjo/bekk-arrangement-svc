FROM fsharp:10.2.1-netcore AS build-env
WORKDIR /app

COPY src/ .
RUN dotnet publish -c Release -o out

FROM microsoft/dotnet:2.1-aspnetcore-runtime

COPY --from=build-env /app/out .

ENV VIRTUAL_PATH="/arrangment-svc"
ENV PORT=80
CMD dotnet arrangementSvc.dll