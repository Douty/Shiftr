function BenefitsSection() {
  return (
    <section aria-labelledby="benefits-title" className="mb-12">
      <h2
        id="benefits-title"
        className="mb-6 font-display text-[clamp(1.75rem,4vw,2.25rem)] font-semibold leading-[1.15] tracking-[-0.01em]"
      >
        Made for both sides of the front door
      </h2>

      <div className="grid grid-cols-1 gap-5 md:grid-cols-2 md:gap-6">
        <article
          aria-labelledby="resident-title"
          className="rounded-3xl border-2 border-brand bg-brand p-6 text-white sm:p-9"
        >
          <h3
            id="resident-title"
            className="mb-2 font-display text-[1.625rem] font-semibold leading-tight"
          >
            For residents
          </h3>
          <p className="mb-5">
            Everything you need from your building, in one place.
          </p>
          <ul className="m-0 grid list-none gap-[1.1rem] p-0">
            <li className="relative pl-7">
              <span aria-hidden="true" className="absolute left-0 top-[0.55em] h-3 w-3 rounded-full bg-blush" />
              <strong className="block font-semibold">Send service requests</strong>
              Report a repair or ask for help in a few taps, and let your team
              know exactly what you need.
            </li>
            <li className="relative pl-7">
              <span aria-hidden="true" className="absolute left-0 top-[0.55em] h-3 w-3 rounded-full bg-blush" />
              <strong className="block font-semibold">Book amenities</strong>
              Reserve the shared spaces in your building without a phone call
              or a sign-up sheet.
            </li>
            <li className="relative pl-7">
              <span aria-hidden="true" className="absolute left-0 top-[0.55em] h-3 w-3 rounded-full bg-blush" />
              <strong className="block font-semibold">Control your guest list</strong>
              Update your preferences for which guests are allowed and which
              are not, whenever things change.
            </li>
          </ul>
        </article>

        <article
          aria-labelledby="employee-title"
          className="rounded-3xl border-2 border-brand bg-transparent p-6 text-ink sm:p-9"
        >
          <h3
            id="employee-title"
            className="mb-2 font-display text-[1.625rem] font-semibold leading-tight"
          >
            For employees
          </h3>
          <p className="mb-5">Keep the whole team informed, shift after shift.</p>
          <ul className="m-0 grid list-none gap-[1.1rem] p-0">
            <li className="relative pl-7">
              <span aria-hidden="true" className="absolute left-0 top-[0.55em] h-3 w-3 rounded-full bg-accent" />
              <strong className="block font-semibold">Detailed activity log</strong>
              Record day-to-day activities in one shared place, so nothing
              lives only in someone&apos;s memory.
            </li>
            <li className="relative pl-7">
              <span aria-hidden="true" className="absolute left-0 top-[0.55em] h-3 w-3 rounded-full bg-accent" />
              <strong className="block font-semibold">Effective shift handoffs</strong>
              Pass along what is finished, what is still open, and what needs
              attention next.
            </li>
            <li className="relative pl-7">
              <span aria-hidden="true" className="absolute left-0 top-[0.55em] h-3 w-3 rounded-full bg-accent" />
              <strong className="block font-semibold">Easier transitions</strong>
              The incoming shift starts with the full picture instead of
              piecing it together.
            </li>
          </ul>
        </article>
      </div>
    </section>
  )
}

export default BenefitsSection