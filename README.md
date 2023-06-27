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

<!-- start outputs -->
<!-- end outputs -->
