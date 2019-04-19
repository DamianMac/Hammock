# Build image
FROM hammock-build:latest AS build
FROM build as test

ENV HAMMOCK_TEST_DB="couchdb-test"
WORKDIR /sln

ENTRYPOINT [ "/bin/bash", "./build.sh", "--target=test" ]