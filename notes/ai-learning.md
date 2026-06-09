## AI-Supplemented Learning: A Working List


*Ways a developer can use an AI coding assistant as a learning tool — not just a code generator. This is a list to maintain, add to, demonstrate in class, and revisit as the tools evolve.*

### Building a Mental Model


- **Explanation on demand.** "Explain what this code does." Then: "That didn't click — explain it a different way." Then: "Give me an analogy." The AI has infinite patience and can reframe indefinitely until something lands.

- **Mental model verification.** "I think X works like Y — is that right? Where does my mental model break down?" This is underused. Most developers either assume they understand something or don't examine the assumption at all.

- **Historical/motivational context.** "Why does React's state model work this way? What problem was it solving?" Understanding *why* a design exists is often more durable than understanding *what* it does.

- **Comparative explanation.** "Show me three ways to solve this problem and explain the tradeoffs." Forces exposure to a design space rather than a single answer, and builds the judgment to choose between them.
### Learning from Failure

- **Deliberate breaking.** Write or generate something that works, then ask: "What are the ways this could fail? Show me an example of each." This is particularly valuable for things like React, where a developer can get surprisingly far without understanding the DOM — until something breaks in a confusing way.

- **Failure mode study.** When something breaks, don't just ask the AI to fix it. Ask it to explain *why* it broke and what mental model would have predicted the failure. The fix is cheap; the model is the asset.

- **"What am I not seeing?"** Show the AI working code and ask: "What subtle problems or edge cases could bite me later that aren't obvious right now?" This surfaces the category of issues that only experience usually reveals.
### Deepening Understanding

- **Go deeper on something you're using but don't really understand.** "I've been using `useEffect` for months but I think I'm cargo-culting it. Help me actually understand it." Developers routinely use things that work without understanding them — AI makes it easy to close those gaps on demand.

- **Concept mapping.** "I just learned about X. What other concepts are closely related that I should understand to use X well?" Helps build connected knowledge rather than isolated facts.

- **Ask it to teach you, then quiz you.** "Explain closures to me, then ask me some questions to test whether I've understood." Active recall is more effective than passive reading.
### Personalized Learning

- **Learning plan generation.** Share your code or describe your work, then ask: "What concepts, patterns, or language features would most improve the quality of what I'm building? Give me a prioritized learning guide." This can produce surprisingly tailored, detailed results.

- **"What should I learn next?"** Based on current skill level and goals, an AI can suggest a path that would be impractical to construct from generic curricula.

- **Pre-class preparation.** Before attending a training session or reading documentation, ask the AI for an orientation: "I'm about to learn about X. What's the core concept, what's the hardest part to understand, and what prior knowledge should I make sure I have?"
### Scaffolded Practice

- **Write it yourself first, then compare.** Attempt a solution without AI help, then ask the AI to solve the same problem and explain any differences. The comparison is often more instructive than either approach alone.

- **AI as reviewer, not author.** Write the code yourself, then ask the AI to review it and explain what could be better and why. Keeps the developer in the production role while introducing feedback that would otherwise require a senior colleague.

- **Use AI to build scaffolding, then study it.** Generate a working example of something unfamiliar, then work through it carefully to understand every part before trying to write it yourself. Useful when the gap between "nothing" and "first working version" is discouraging.

- **Challenge AI's choices to test your own.** When AI solves something differently than you would, resist the instinct to simply override it. Instead ask: "I tend to do this a different way — are there advantages to your approach, or disadvantages to mine, I should consider?" This works because it's organic and in-context — triggered by real code you're writing, not a hypothetical. The result is either a genuine update to your practice, or increased confidence that your preference is well-founded.

### Using AI to Explore the Problem Space

- **Rapid iteration to find the right solution.** AI is well-suited to the "finding out" phase of development — quickly generating alternatives, prototypes, and variations so you can evaluate options before committing. The criticism that "AI doesn't write maintainable code" often misses the point: *the expensive mistake isn't writing messy code, it's writing clean code for the wrong thing.* Optimization for maintainability belongs to the phase after you know what you're building. AI accelerates getting there.

- **A/B testing approaches.** Generate two or three implementations of the same thing with different tradeoffs, compare them, and decide. This is faster with AI than any other approach and builds real judgment about design decisions rather than just defaulting to habit.

---

*Note: the value of writing code by hand — even when AI can generate it — is probably not about the output. It's about building the mental models, and especially the failure intuitions, that make you capable of evaluating AI-generated code. The question of how much hand-writing is needed to build a sufficient model is likely to keep changing.*

---

## A Note on Framing

One underappreciated gift AI offers is making code less precious. When generation is cheap, the real work — understanding the problem, making the right tradeoffs, knowing when something is good enough — moves back to the center where it always belonged.

*(There is a sharper version of this observation (if the statement above raised your blood pressure, maybe just skip this one) about teams that elevated the craft above the purpose and are now surprised to find themselves displaced. It's probably true. It's also probably not the version that changes anyone's mind. AI turns out to be useful here too: one of its quieter skills is helping you find the form of a true thing that people can actually hear.)*

---

*These ideas were developed in conversation with Claude. It would be a strange kind of dishonesty to write about AI-assisted thinking while hiding that I used it.*