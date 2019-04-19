#!/usr/bin/env bash

docker-compose build --force-rm --build-arg BUILD_NUMBER
docker-compose up hammock-build && docker-compose up --exit-code-from hammock-test hammock-test
if [ $? -eq 0 ]
then
    docker-compose up hammock-publish
fi
docker-compose down