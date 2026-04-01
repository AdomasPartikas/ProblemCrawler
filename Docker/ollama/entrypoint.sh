#!/bin/sh

ollama serve 2>&1 &

echo "Waiting for Ollama to start..."
until curl -s http://localhost:11434/api/tags > /dev/null 2>&1; do
  sleep 2
done

ollama pull qwen3:8b || true

echo "Models ready."
wait