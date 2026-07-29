## About this project
* A cross-platform video game for Android and PC based on **[Unity3D](https://www.unity.com
)**.
* Streamlined the development process by integrating **[OpenAI APIs](https://platform.openai.com/docs/overview)** with the **[Continue.dev](https://docs.continue.dev/)** plugin.
* Customized **[Stable Diffusion](https://en.wikipedia.org/wiki/Stable_Diffusion)** models on a local machine to generate consistent UI resources.
### Contents
* [Resources](#resources)
* [Demo](#demo)
* [Character Design](#character-design)


### Resources
* []()
* [Unity Asset Store:  2d-environment](https://assetstore.unity.com/lists/2d-environment-3574226613018)
* [Unity Asset Store:  2d-UI](https://assetstore.unity.com/lists/2d-ui-3574226717596)
* [Unity Asset Store:  2d-character](https://assetstore.unity.com/lists/2d-character-3574226676604)
* [Unity Asset Store:  Sound](https://assetstore.unity.com/lists/sound-3574226651147)
* [Unity Docs](https://docs.unity3d.com/Manual/class-InputManager.html)
* [2D Art Generator](https://itch.io/queue/c/1866035/pixel-art-generators?game_id=821003)

### Local AI agent setup
Link to this agent: [Continue.dev](https://hub.continue.dev/ryan-elden/ryan-elden-first-assistant?view=config)
```YAML
name: Unity Assistant
version: 0.0.12
schema: v1
models:
  - uses: openai/gpt-4.1
    with:
      OPENAI_API_KEY: ${{ secrets.OPENAI_API_KEY }}
  - uses: ollama/qwen2.5-coder-1.5b
  - uses: ryan-elden/autocomplete
  - uses: ryan-elden/deepseek-coder13b
  - uses: ryan-elden/openaigpt-4o
    with:
      OPENAI_API_KEY: ${{ secrets.OPENAI_API_KEY }}
context:
  - uses: continuedev/code-context
  - uses: continuedev/docs-context
  - uses: continuedev/diff-context
  - uses: continuedev/terminal-context
  - uses: continuedev/problems-context
  - uses: continuedev/folder-context
  - uses: continuedev/codebase-context
rules:
  - uses: genomagames/unity-rules
prompts:
  - uses: starter/test-prompt
docs:
  - uses: genomagames/unity-docs
  - uses: genomagames/c-sharp
```
### Stable Diffusion and Control Net

<p align="center">
  <img  height="450" width="720"src="https://github.com/Grindewald1900/Notebook/blob/master/Image/Galactic/StableDiffusion.png">    
</p>  
<p align="center">
  <img  height="450" width="720"src="https://github.com/Grindewald1900/Notebook/blob/master/Image/Galactic/forward_first.png">    
</p>  

### Demo
* **Main Menu**

<p align="center">
  <img src="https://github.com/Grindewald1900/Notebook/blob/master/Image/Galactic/main.gif">    
</p>  

* **Load Game**

<p align="center">
  <img src="https://github.com/Grindewald1900/Notebook/blob/master/Image/Galactic/load.gif">    
</p>  

* **Draw Cards**
<p align="center">
  <img src="https://github.com/Grindewald1900/Notebook/blob/master/Image/Galactic/draw.gif">    
</p>  

* **Draw Summary**
<p align="center">
  <img src="https://github.com/Grindewald1900/Notebook/blob/master/Image/Galactic/cards.gif">    
</p>  

* **Cards**
<p align="center">
  <img src="https://github.com/Grindewald1900/Notebook/blob/master/Image/Galactic/cards3.gif">       
</p>  

* **Filters**
<p align="center">
  <img src="https://github.com/Grindewald1900/Notebook/blob/master/Image/Galactic/cards2.gif">   
</p>  

* **Build Lineup**
<p align="center">
  <img src="https://github.com/Grindewald1900/Notebook/blob/master/Image/Galactic/lineup2.gif">    
</p>  

<p align="center">
  <img src="https://github.com/Grindewald1900/Notebook/blob/master/Image/Galactic/lineup3.gif">    
</p>  

* **Explore**
<p align="center">
  <img src="https://github.com/Grindewald1900/Notebook/blob/master/Image/Galactic/explore.gif">    
</p>  

* **Battle**
<p align="center">
  <img src="https://github.com/Grindewald1900/Notebook/blob/master/Image/Galactic/battle.gif">    
</p>  

* **Inventory**
<p align="center">
  <img src="https://github.com/Grindewald1900/Notebook/blob/master/Image/Galactic/inventory.gif">    
</p>  


### Character Design
<p align="center">
  <img width="200" height="240" src="https://github.com/Grindewald1900/Notebook/blob/master/Image/Galactic/Asra_01.png">    
  <img width="200" height="240" src="https://github.com/Grindewald1900/Notebook/blob/master/Image/Galactic/Magki_01.png">    
  <img width="200" height="240" src="https://github.com/Grindewald1900/Notebook/blob/master/Image/Galactic/Sernia_01.png">    
  <img width="200" height="240" src="https://github.com/Grindewald1900/Notebook/blob/master/Image/Galactic/Ibalon.png">    
  
</p>  

* **Asra**
<p align="center">
  <img  height="300" width="720" src="https://github.com/Grindewald1900/Notebook/blob/master/Image/Galactic/Asra1.png">    
</p>  

<p align="center">
  <img  height="300" width="720"src="https://github.com/Grindewald1900/Notebook/blob/master/Image/Galactic/Asra2.png">    
</p>  


* **Magki**
<p align="center">
  <img  height="300" width="720"src="https://github.com/Grindewald1900/Notebook/blob/master/Image/Galactic/Magki1.png">    
</p>  

<p align="center">
  <img  height="300" width="720" src="https://github.com/Grindewald1900/Notebook/blob/master/Image/Galactic/Magki2.png">    
</p>  

* **Sernia**
<p align="center">
  <img  height="300" width="720" src="https://github.com/Grindewald1900/Notebook/blob/master/Image/Galactic/Sernia1.png">    
</p>  

<p align="center">
  <img  height="300" width="720" src="https://github.com/Grindewald1900/Notebook/blob/master/Image/Galactic/Sernia2.png">    
</p>  

* **Ibalon**
<p align="center">
  <img  height="300" width="720" src="https://github.com/Grindewald1900/Notebook/blob/master/Image/Galactic/Ibalon1.png">    
</p>  

<p align="center">
  <img  height="300" width="720" src="https://github.com/Grindewald1900/Notebook/blob/master/Image/Galactic/Ibalon2.png">    
</p>  
