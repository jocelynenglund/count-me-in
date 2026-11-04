# How Count me in was built

Shots and output from each step, in order, matching the commits.

## 1. The model, refined in Event Book

Six slices in three chapters, each with Given/When/Then scenarios, happy path first. Every slice is ticked Refined before anything gets built.

![The board](../model/mvp.png)

Commit: *Model: Count me in MVP, refined in Event Book*

## 2. ItsyBOT takes the plan

While the builder (ItsyBOT) works on the plan, it's checked out to it, so the board is read-only on the phone, with a *Take back* button.

![ItsyBOT is editing, read only here](02-itsybot-editing.jpg)

## 3. Specs from the model, all red

Event Book writes one spec per refined slice, and a stub beside each that throws `NotBuiltYet`.

```
$ npm test                  # pending: CI stays green while nothing is built
ℹ tests 19
ℹ pass 0
ℹ fail 0
ℹ todo 19

$ npm run test:red          # GWT_RUN_PENDING=1: the same specs, failing
ℹ tests 19
ℹ pass 0
ℹ fail 19
  NotBuiltYet: not built yet
```

Commit: *Specs: Given/When/Then from the model, all red*

## 4. One slice at a time

| Commit | Slice | Green |
|---|---|---|
| Build S1 Start Interest Check | S1 | 4 of 19 |
