dotnet test

allure generate ./bin/Debug/net9.0/allure-results --clean -o allure-report

allure open allure-report