# Azure Cognitive Search CLI

Allowing you to index blog posts right from you pipeline.

<!-- start title -->

# GitHub Action: Azure Cognitive Search Index

<!-- end title -->
<!-- start description -->

Index local json files and send them to your Azure Cognitive Search Service

<!-- end description -->
<!-- start contents -->
<!-- end contents -->

## Using this action

<!-- start usage -->

```yaml
- uses: svrooij/az-cog-cli@main
  with:
    # The location of the folder to index
    folder: ""

    # The endpoint of your Azure Search Service
    endpoint: ""

    # The Admin API Key, needed for uploading documents
    apiKey: ""

    # What files to look for
    # Default: index.json
    query: ""

    # IndexName to send the items to
    # Default: blog-1
    index: ""
```

<!-- end usage -->

## Inputs

<!-- start inputs -->

| **Input**      | **Description**                                   | **Default**  | **Required** |
| -------------- | ------------------------------------------------- | ------------ | ------------ |
| **`folder`**   | The location of the folder to index               |              | **true**     |
| **`endpoint`** | The endpoint of your Azure Search Service         |              | **true**     |
| **`apiKey`**   | The Admin API Key, needed for uploading documents |              | **true**     |
| **`query`**    | What files to look for                            | `index.json` | **false**    |
| **`index`**    | IndexName to send the items to                    | `blog-1`     | **false**    |

<!-- end inputs -->

Set the `endpoint` and `apiKey` through secrets e.g. `endpoint: ${{ secrets.AZURE_SEARCH_ENDPOINT }}`

## Docker: Azure Cognitive Search Index

This app is also published as a docker container, you can run as follows.

Use `{platform-specific-part} index folder {location-of-mapped-folder} --endpoint https://{searchService}.search.windows.net --apiKey xxx`

Or run `docker run --rm ghcr.io/svrooij/az-cog-cli:dev index folder --help` for more details.

```text
> docker run --rm ghcr.io/svrooij/az-cog-cli:dev index folder --help
Description:
  Add data in a folder to your index

Usage:
  az-cog-cli index folder <folder> [options]

Arguments:
  <folder>  Folder to look for files

Options:
  --query <query>        What files to look for [default: index.json]
  --index <index>        Which index shall be used? [default: blog-1]
  --endpoint <endpoint>  Specify the endpoint
  --apiKey <apiKey>      Specify the Admin API Key
  -?, -h, --help         Show help and usage information
```

### Windows (using PowerShell)

```PowerShell
docker run --rm -v ${PWD}:/usr/my-folder ghcr.io/svrooij/az-cog-cli index folder /usr/my-folder --endpoint https://{searchService}.search.windows.net --apiKey xxx
```

### Linux

```bash
docker run --rm -v $(pwd):/usr/my-folder ghcr.io/svrooij/az-cog-cli index folder /usr/my-folder --endpoint https://{searchService}.search.windows.net --apiKey xxx
```
