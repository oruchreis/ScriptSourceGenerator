//HintName: NugetTest.GeneratedModels.cs
{
  "swagger": "2.0",
  "info": {
    "title": "Swagger Petstore",
    "license": {
      "name": "MIT"
    },
    "version": "1.0.0"
  },
  "host": "petstore.swagger.io",
  "basePath": "/v1",
  "schemes": [
    "http"
  ],
  "paths": {
    "/pets": {
      "get": {
        "tags": [
          "pets"
        ],
        "summary": "List all pets",
        "operationId": "listPets",
        "produces": [
          "application/json"
        ],
        "parameters": [
          {
            "in": "query",
            "name": "limit",
            "description": "How many items to return at one time (max 100)",
            "type": "integer",
            "format": "int32",
            "maximum": 100
          }
        ],
        "responses": {
          "200": {
            "description": "A paged array of pets",
            "schema": {
              "$ref": "#/definitions/Pets"
            },
            "headers": {
              "x-next": {
                "description": "A link to the next page of responses",
                "type": "string"
              }
            }
          },
          "default": {
            "description": "unexpected error",
            "schema": {
              "$ref": "#/definitions/Error"
            }
          }
        }
      },
      "post": {
        "tags": [
          "pets"
        ],
        "summary": "Create a pet",
        "operationId": "createPets",
        "produces": [
          "application/json"
        ],
        "responses": {
          "201": {
            "description": "Null response"
          },
          "default": {
            "description": "unexpected error",
            "schema": {
              "$ref": "#/definitions/Error"
            }
          }
        }
      }
    },
    "/pets/{petId}": {
      "get": {
        "tags": [
          "pets"
        ],
        "summary": "Info for a specific pet",
        "operationId": "showPetById",
        "produces": [
          "application/json"
        ],
        "parameters": [
          {
            "in": "path",
            "name": "petId",
            "description": "The id of the pet to retrieve",
            "required": true,
            "type": "string"
          }
        ],
        "responses": {
          "200": {
            "description": "Expected response to a valid request",
            "schema": {
              "$ref": "#/definitions/Pet"
            }
          },
          "default": {
            "description": "unexpected error",
            "schema": {
              "$ref": "#/definitions/Error"
            }
          }
        }
      }
    }
  },
  "definitions": {
    "Pet": {
      "required": [
        "id",
        "name"
      ],
      "type": "object",
      "properties": {
        "id": {
          "format": "int64",
          "type": "integer"
        },
        "name": {
          "type": "string"
        },
        "tag": {
          "type": "string"
        }
      }
    },
    "Pets": {
      "maxItems": 100,
      "type": "array",
      "items": {
        "$ref": "#/definitions/Pet"
      }
    },
    "Error": {
      "required": [
        "code",
        "message"
      ],
      "type": "object",
      "properties": {
        "code": {
          "format": "int32",
          "type": "integer"
        },
        "message": {
          "type": "string"
        }
      }
    }
  }
}