#!/usr/bin/env bash

dotnet test --blame-crash --blame-hang --blame-hang-timeout 4m --logger "trx;LogFileName=TestResults.trx" -p:ParallelizeTestCollections=true --collect:"XPlat Code Coverage" --results-directory artifacts/test_results