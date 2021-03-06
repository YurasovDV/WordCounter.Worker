# Dockerfile
FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build-env
WORKDIR /src
COPY ["WordCounter.Worker.csproj", "."]
COPY ["WordCounter.Common/WordCounter.Common.csproj", "./WordCounter.Common/"]
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1
WORKDIR /app
COPY --from=build-env /app .

ENV RabbitMqHost rabbit_mq
ENV RabbitMqPort 5672
ENV RabbitMqUser admin
ENV RabbitMqPass demo

ENV DbHost db
ENV DbPort 5432
ENV DbUser admin
ENV DbPass demo
ENV Db counters

ENV ElasticHost elasticsearch
ENV ElasticPort 9200
ENV ElasticIndex ApiWorkerIndex

ENTRYPOINT ["dotnet", "WordCounter.Worker.dll"]