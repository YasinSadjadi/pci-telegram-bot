FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY VocabularyBot.Domain/VocabularyBot.Domain.csproj VocabularyBot.Domain/
COPY VocabularyBot.Infrastructure/VocabularyBot.Infrastructure.csproj VocabularyBot.Infrastructure/
COPY VocabularyBot.Application/VocabularyBot.Application.csproj VocabularyBot.Application/
COPY VocabularyBot.Bot/VocabularyBot.Bot.csproj VocabularyBot.Bot/
RUN dotnet restore VocabularyBot.Bot/VocabularyBot.Bot.csproj
COPY VocabularyBot.Domain/ VocabularyBot.Domain/
COPY VocabularyBot.Infrastructure/ VocabularyBot.Infrastructure/
COPY VocabularyBot.Application/ VocabularyBot.Application/
COPY VocabularyBot.Bot/ VocabularyBot.Bot/
RUN dotnet publish VocabularyBot.Bot/VocabularyBot.Bot.csproj -c Release --no-restore -o /out /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
RUN mkdir /data && chown app:app /data
COPY --from=build /out/ ./
ENV DOTNET_ENVIRONMENT=Production BOT_DATA_DIR=/data
USER app
ENTRYPOINT ["dotnet", "VocabularyBot.Bot.dll"]
