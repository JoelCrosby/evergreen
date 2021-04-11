#!/bin/sh

dotnet publish Evergreen/Evergreen.csproj -c Release -r linux-x64 /p:PublishTrimmed=true --self-contained
