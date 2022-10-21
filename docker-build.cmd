set version=1.1.0

docker build --no-cache --force-rm -t monitoring-pods:%version% --build-arg VERSION=%version% -f Dockerfile .
docker tag monitoring-pods:%version% mentalistdev/monitoring-pods:%version%
docker push mentalistdev/monitoring-pods:%version%

docker image prune --filter label=stage=build -f
