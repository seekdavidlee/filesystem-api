# Introduction

This is an example of using redis as a file system api.

```bash
docker build -f ./src/Dockerfile -t filesystemapi .
docker run -d -p 80:80 filesystemapi
```