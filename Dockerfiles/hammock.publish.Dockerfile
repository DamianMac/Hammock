# Build image
FROM hammock-build:latest AS build
FROM build as publish

WORKDIR /sln

ENTRYPOINT [ "/bin/bash", "./build.sh", "--target=PushPackages" ]