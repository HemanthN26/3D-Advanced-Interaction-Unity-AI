# Chatbot Interaction in VR with Unity

An immersive Unity VR prototype where users interact with an conversational AI avatar using text and voice inside a 3D environment.

---

## Demo Preview

### VR Environment Overview

<img width="797" height="422" alt="image" src="https://github.com/user-attachments/assets/035c1291-83fe-430f-8024-6db5c58534a5" />

### Interaction and Input System

![5ec5ba8a-d719-4b29-8f0d-2201e2f51571](https://github.com/user-attachments/assets/3a0f199e-5182-426b-8c85-f27b6dfb67ce)

### AI Interaction Demo

https://github.com/user-attachments/assets/201323e9-fa3f-43b2-9f4d-06ecaa7bb3fc

---

## Project Overview

This project explores the integration of artificial intelligence and immersive interaction by combining:

- Virtual Reality (VR)
- Large Language Models (LLMs)
- 3D Avatars and interaction design
- Real-time UI and multimodal communication in Unity

The objective is to investigate how conversational AI can be embedded into 3D virtual environments to enable natural and interactive human-AI communication beyond traditional chat interfaces.

---

## Features

- AI chatbot integrated with Unity
- VR environment built in Unity 6
- AI avatar using Ready Player Me
- World-space chat UI with history
- Voice input and text input support
- Modular architecture (Unity -> Backend -> LLM)

---

## Tech Stack

**Hardware:**

- Meta Quest 3

**Development:**

- Unity 6
- Meta XR SDK

**AI & Backend:**

- LLM via Groq API (LLaMA / Mistral)
- Whisper (Speech-to-Text)
- Text-to-Speech (TTS)

**Avatar System:**

- Ready Player Me

---

## System Architecture

User interaction in VR is processed through Unity’s UI and interaction layer, which communicates with a Python backend handling speech and AI services. Generated responses are returned to Unity and rendered through the avatar and interface.

This modular architecture allows flexible experimentation with AI models and interaction techniques.

---

This project was developed as part of the Three-Dimensional and Advanced Interaction course, focusing on the design and implementation of interactive AI systems in immersive environments.


