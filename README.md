# Count me in

Measure interest in an idea before you do it. Share a pitch with a goal ("10 people would make this worth doing"), people tap **Count me in**, and you watch the count and how fast it grows.

## From model to production

This repo is built in the open, one step per commit, so you can follow how an itsybit app gets from an event model to production:

1. **The model.** Planned and refined in Event Book: [Count me in · plan](https://eventpad.itsybit.se/?book=m_mw2g5ovjfwjvy). The agreed plan is in `docs/model/`.
2. **The specs, red.** Every refined slice becomes Given/When/Then tests before any code exists.
3. **One slice at a time, green.** Each commit makes one slice's scenarios pass.
4. **Production.** The deploy runs the tests and publishes the build book next to the plan.

![The MVP model: three chapters, six slices](docs/model/mvp.png)

The build, step by step with shots: [docs/process](docs/process/README.md).
