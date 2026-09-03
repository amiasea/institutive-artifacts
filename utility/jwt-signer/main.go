package main

import (
	"context"
	"crypto/sha256"
	"encoding/json"
	"errors"
	"fmt"
	"log"
	"net/url"
	"os"
	"strings"
	"time"

	"github.com/Azure/azure-sdk-for-go/sdk/azidentity"
	"github.com/Azure/azure-sdk-for-go/sdk/security/keyvault/azkeys"
	"github.com/golang-jwt/jwt/v5"
)

type Response struct {
	Token string `json:"token"`
}

type keyVaultSigningMethod struct {
	client  *azkeys.Client
	name    string
	version string
}

func (m *keyVaultSigningMethod) Alg() string {
	return "RS256"
}

func (m *keyVaultSigningMethod) Sign(signingString string, _ any) ([]byte, error) {
	digest := sha256.Sum256([]byte(signingString))

	algorithm := azkeys.SignatureAlgorithmRS256

	resp, err := m.client.Sign(
		context.Background(),
		m.name,
		m.version,
		azkeys.SignParameters{
			Algorithm: &algorithm,
			Value:     digest[:],
		},
		nil,
	)
	if err != nil {
		return nil, fmt.Errorf("sign digest with Key Vault: %w", err)
	}

	if len(resp.Result) == 0 {
		return nil, errors.New("Key Vault returned an empty signature")
	}

	return resp.Result, nil
}

func (m *keyVaultSigningMethod) Verify(_ string, _ []byte, _ any) error {
	return errors.New("verification is not supported")
}

func parseKeyURI(keyURI string) (string, string, string, error) {
	parsed, err := url.Parse(keyURI)
	if err != nil || parsed.Scheme != "https" || parsed.Host == "" {
		return "", "", "", errors.New("KEY_URI must be an HTTPS Key Vault key URI")
	}

	parts := strings.Split(strings.Trim(parsed.Path, "/"), "/")

	if len(parts) != 3 ||
		parts[0] != "keys" ||
		parts[1] == "" ||
		parts[2] == "" {
		return "", "", "", errors.New(
			"KEY_URI must have the form https://<vault>/keys/<name>/<version>",
		)
	}

	return parsed.Scheme + "://" + parsed.Host, parts[1], parts[2], nil
}

func main() {
	keyURI := os.Getenv("KEY_URI")
	if keyURI == "" {
		log.Fatal("KEY_URI environment variable is required")
	}

	iss := os.Getenv("ISS")
	if iss == "" {
		log.Fatal("ISS environment variable is required")
	}

	vaultURI, keyName, keyVersion, err := parseKeyURI(keyURI)
	if err != nil {
		log.Fatal(err)
	}

	cred, err := azidentity.NewDefaultAzureCredential(nil)
	if err != nil {
		log.Fatalf("create Azure credential: %v", err)
	}

	client, err := azkeys.NewClient(vaultURI, cred, nil)
	if err != nil {
		log.Fatalf("create Key Vault client: %v", err)
	}

	method := &keyVaultSigningMethod{
		client:  client,
		name:    keyName,
		version: keyVersion,
	}

	now := time.Now()

	token := jwt.NewWithClaims(
		method,
		jwt.MapClaims{
			"iat": now.Add(-60 * time.Second).Unix(),
			"exp": now.Add(10 * time.Minute).Unix(),
			"iss": iss,
		},
	)

	signedToken, err := token.SignedString(nil)
	if err != nil {
		log.Fatalf("sign JWT: %v", err)
	}

	if err := json.NewEncoder(os.Stdout).Encode(Response{
		Token: signedToken,
	}); err != nil {
		log.Fatalf("write response: %v", err)
	}
}
