#FROM microsoft/dotnet:3.0-sdk AS build-env
FROM microsoft/dotnet:2.1-sdk AS build-env
WORKDIR /app

# Copy csproj and restore as distinct layers
# COPY ./*.fsproj ./
# RUN dotnet restore

# Copy everything else and build
COPY src/testApp .
RUN dotnet publish -c Release -o out

# Build runtime image
#FROM microsoft/dotnet:3.0
#FROM microsoft/dotnet:3.0-aspnetcore-runtime
FROM microsoft/dotnet:2.1-aspnetcore-runtime

COPY --from=build-env /app/out .

ENV VIRTUAL_PATH="/arrangment-svc"
ENV PORT=80

CMD dotnet testApp.dll
