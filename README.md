# Mahjong Matching Puzzle (Unlock)

## Overview

A grid-based puzzle game inspired by Mahjong. It is originated in Hunan, China.
The player is presented with a randomized board of tiles and must clear all tiles using matching and movement mechanics.

---

## Game Rules

### Board Setup

- The game is played on a grid-based board (default size: 17 × 8).
- Each cell contains either:
  - a **tile** (face-up Mahjong piece), or
  - a **path** (fliped / cleared tile).
- All elements are aligned to fixed grid positions.

---

### Rule 1 — Tile Matching

Two tiles can be cleared if:

- They are aligned on the same row or column, and  
- There are no tiles between them (only path or empty space)

When matched:

- Both tiles are flipped and become **path**
- The cleared space can be used for future movement

---

### Rule 2 — Push Movement

A selected tile (referred to as the **active tile**) can be moved in one direction (up, down, left, right) under the following conditions:

- All directly connected tiles in that direction move together as a group  
- The group can only move if there is a continuous path ahead (no blocking tiles)  
- The group can move to the furthest valid position along that path

After the movement:

- Only the **active tile** must satisfy Rule 1 with another tile  
- If no valid match is formed, the move is **invalid and reverted**

---

### Objective

- Clear all tiles from the board using valid moves

---

## Design Intent

This project rebuilds an earlier prototype with a focus on:

- clean separation of gameplay systems  
- reliable grid-based movement logic  
- accurate move validation and reversal  
- improved input handling and visual feedback  

---

## Technical Design

_tba_

---

## How to Run

_tba_

---

## Demo (Old Prototype)

https://www.youtube.com/embed/t9qz355X4n8?si=Q087Ax0k_4AKgBj1