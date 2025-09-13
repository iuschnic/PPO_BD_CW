FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src

# Копируем исходный код
COPY ["lab3/code/Domain/", "Domain/"]
COPY ["lab3/code/Storage/", "Storage/"]
COPY ["lab3/code/ShedLoadAdapter/", "ShedLoadAdapter/"]
COPY ["lab3/code/Tests/", "Tests/"]

# Устанавливаем Allure
RUN apt-get update && \
    apt-get install -y default-jre && \
    curl -o allure-2.27.0.tgz -Ls https://github.com/allure-framework/allure2/releases/download/2.27.0/allure-2.27.0.tgz && \
    tar -zxvf allure-2.27.0.tgz -C /opt/ && \
    ln -s /opt/allure-2.27.0/bin/allure /usr/bin/allure

# Восстанавливаем и строим только нужные проекты
RUN dotnet restore "Domain/Domain.csproj" && \
    dotnet restore "Storage/Storage.csproj" && \
	dotnet restore "ShedLoadAdapter/ShedLoadAdapter.csproj" && \
    dotnet restore "Tests/Tests.csproj"

RUN dotnet build "Domain/Domain.csproj" -c Release --no-restore && \
    dotnet build "Storage/Storage.csproj" -c Release --no-restore && \
	dotnet build "ShedLoadAdapter/ShedLoadAdapter.csproj" -c Release --no-restore && \
    dotnet build "Tests/Tests.csproj" -c Release --no-restore

# Запуск тестов
ENTRYPOINT ["dotnet", "test", "Tests/Tests.csproj", "-c", "Release", "--no-build"]