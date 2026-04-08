#!/bin/sh

ollama serve 2>&1 &

echo "Waiting for Ollama to start..."
until curl -s http://localhost:11434/api/tags > /dev/null 2>&1; do
  sleep 2
done

MODELS="${OLLAMA_MODELS:-qwen3:8b}"

echo "Pulling models: $MODELS"
echo "$MODELS" | tr ',' '\n' | while read -r model; do
  model=$(echo "$model" | tr -d '[:space:]')
  echo "Pulling $model..."
  ollama pull "$model" || echo "Warning: failed to pull $model"
done

echo "Models ready."
wait