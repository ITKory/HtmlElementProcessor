# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS app
WORKDIR /src
COPY HtmlElementProcessor.csproj ./
RUN dotnet restore HtmlElementProcessor.csproj
EXPOSE 8090
ENV ASPNETCORE_URLS=http://+:8090
CMD ["dotnet", "run", "HtmlElementProcessor.csproj", "--urls", "http://0.0.0.0:8090"]
