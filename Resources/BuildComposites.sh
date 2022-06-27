#!/usr/bin/env bash

RESOURCES_DIR=$(pwd)

cd BuildEngine
dotnet build BuildEngine.csproj -c Release

BUILD_ENGINE=$(find . -name BuildEngine -type f)
$BUILD_ENGINE $RESOURCES_DIR
