FROM golang:1.26 AS build

WORKDIR /src

COPY go.mod go.sum ./

RUN go mod download

COPY . .

RUN CGO_ENABLED=0 GOOS=linux GOARCH=amd64 \
    go build -trimpath -ldflags="-s -w" -o /jwt-signer .

FROM gcr.io/distroless/static-debian12

COPY --from=build /jwt-signer /jwt-signer

USER nonroot:nonroot

EXPOSE 8080

ENTRYPOINT ["/jwt-signer"]