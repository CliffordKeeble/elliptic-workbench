#!/usr/bin/env sh
set -eu

# Fail loudly if an input has moved. A renamed input must break the build,
# not silently publish a partial site.
for f in index.html 404.html web/group-law.html web/quotient-panel.html \
         data/bsd-rank0.json web/_headers; do
  [ -f "$f" ] || { echo "build-site: missing required input: $f" >&2; exit 1; }
done

rm -rf _site
mkdir -p _site/data
cp index.html            _site/
cp 404.html              _site/
cp -R web                _site/web
cp data/bsd-rank0.json   _site/data/
cp web/_headers          _site/_headers

echo "build-site: published $(find _site -type f | wc -l) files"
