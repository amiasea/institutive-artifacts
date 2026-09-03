package main

import (
	"context"
	"crypto"
	"crypto/sha256"
	"encoding/json"
	"errors"
	"fmt"
	"log"
	"net/http"
	"net/url"
	"os"
	"strings"
	"time"

	"github.com/Azure/azure-sdk-for-go/sdk/azidentity"
	"github.com/Azure/azure-sdk-for-go/sdk/security/keyvault/azkeys"
	"github.com/golang-jwt/jwt/v5"
)

type Request struct {
	KeyURI string `json:"keyUri"`
	Iss    string `json:"iss"`
}

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

func (m *keyVaultSigningMethod) Crypto() crypto.Hash {
	return crypto.SHA256
}

func parseKeyURI(keyURI string) (string, string, string, error) {
	parsed, err := url.Parse(keyURI)
	if err != nil || parsed.Scheme != "https" || parsed.Host == "" {
		return "", "", "", errors.New("keyUri must be an HTTPS Key Vault key URI")
	}

	parts := strings.Split(strings.Trim(parsed.Path, "/"), "/")

	if len(parts) != 3 ||
		parts[0] != "keys" ||
		parts[1] == "" ||
		parts[2] == "" {
		return "", "", "", errors.New(
			"keyUri must have the form https://<vault>/keys/<name>/<version>",
		)
	}

	return parsed.Scheme + "://" + parsed.Host, parts[1], parts[2], nil
}

func handler(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodPost {
		http.Error(w, "method not allowed", http.StatusMethodNotAllowed)
		return
	}

	defer r.Body.Close()

	var input Request

	if err := json.NewDecoder(r.Body).Decode(&input); err != nil {
		http.Error(w, "invalid request", http.StatusBadRequest)
		return
	}

	if input.KeyURI == "" {
		http.Error(w, "keyUri is required", http.StatusBadRequest)
		return
	}

	if input.Iss == "" {
		http.Error(w, "iss is required", http.StatusBadRequest)
		return
	}

	vaultURI, keyName, keyVersion, err := parseKeyURI(input.KeyURI)
	if err != nil {
		http.Error(w, err.Error(), http.StatusBadRequest)
		return
	}

	cred, err := azidentity.NewDefaultAzureCredential(nil)
	if err != nil {
		http.Error(
			w,
			"create Azure credential failed",
			http.StatusInternalServerError,
		)
		log.Printf("create Azure credential: %v", err)
		return
	}

	client, err := azkeys.NewClient(vaultURI, cred, nil)
	if err != nil {
		http.Error(
			w,
			"create Key Vault client failed",
			http.StatusInternalServerError,
		)
		log.Printf("create Key Vault client: %v", err)
		return
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
			"iss": input.Iss,
		},
	)

	signedToken, err := token.SignedString(nil)
	if err != nil {
		http.Error(
			w,
			"sign JWT failed",
			http.StatusInternalServerError,
		)
		log.Printf("sign JWT: %v", err)
		return
	}

	w.Header().Set("Content-Type", "application/json")

	if err := json.NewEncoder(w).Encode(Response{
		Token: signedToken,
	}); err != nil {
		log.Printf("write response: %v", err)
	}
}

func main() {
	port := os.Getenv("PORT")
	if port == "" {
		port = "8080"
	}

	mux := http.NewServeMux()
	mux.HandleFunc("/sign", handler)

	server := &http.Server{
		Addr:              ":" + port,
		Handler:           mux,
		ReadHeaderTimeout: 10 * time.Second,
	}

	log.Printf("listening on :%s", port)

	if err := server.ListenAndServe(); err != nil &&
		!errors.Is(err, http.ErrServerClosed) {
		log.Fatal(err)
	}
}
