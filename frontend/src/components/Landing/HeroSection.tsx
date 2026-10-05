type DashboardLink = {
  href: string
  label: string
}

type HeroSectionProps = {
  dashboardLink: DashboardLink | null
  isCheckingSession: boolean
}

function HeroSection({ dashboardLink, isCheckingSession }: HeroSectionProps) {
  return (
    <section
      aria-labelledby="site-title"
      className="grid items-center gap-10 pb-10 pt-12 md:grid-cols-[1.15fr_0.85fr] md:gap-16 md:pb-16 md:pt-24"
    >
      <div>
        <h1
          id="site-title"
          className="font-display text-[clamp(3.75rem,14vw,7.5rem)] font-bold leading-[0.95] tracking-[-0.03em] text-brand"
        >
          Shiftr
        </h1>
        <p className="mt-6 max-w-[30rem] text-xl leading-snug md:text-2xl">
          Clear, friendly communication between the people who live in a
          building and the people who look after it.
        </p>
        <div className="mt-9 flex flex-wrap gap-3.5">
          {dashboardLink ? (
            <a
              href={dashboardLink.href}
              className="inline-flex min-h-[3.25rem] items-center justify-center rounded-xl border-2 border-brand bg-brand px-7 py-3 text-[1.0625rem] font-semibold leading-tight text-white no-underline transition-colors hover:border-brand-dark hover:bg-brand-dark motion-reduce:transition-none focus-visible:outline focus-visible:outline-[3px] focus-visible:outline-offset-[3px] focus-visible:outline-ink"
            >
              {dashboardLink.label}
            </a>
          ) : !isCheckingSession ? (
            <>
              <a
                href="/sign-in/resident"
                className="inline-flex min-h-[3.25rem] items-center justify-center rounded-xl border-2 border-brand bg-brand px-7 py-3 text-[1.0625rem] font-semibold leading-tight text-white no-underline transition-colors hover:border-brand-dark hover:bg-brand-dark motion-reduce:transition-none focus-visible:outline focus-visible:outline-[3px] focus-visible:outline-offset-[3px] focus-visible:outline-ink"
              >
                Resident Sign In
              </a>
              <a
                href="/sign-in/employee"
                className="inline-flex min-h-[3.25rem] items-center justify-center rounded-xl border-2 border-brand bg-blush px-7 py-3 text-[1.0625rem] font-semibold leading-tight text-ink no-underline transition-colors hover:bg-blush-dark motion-reduce:transition-none focus-visible:outline focus-visible:outline-[3px] focus-visible:outline-offset-[3px] focus-visible:outline-ink"
              >
                Employee Sign In
              </a>
            </>
          ) : null}
        </div>
      </div>

      <div aria-hidden="true" className="mx-auto w-full max-w-[22rem]">
        <svg
          viewBox="0 0 320 280"
          xmlns="http://www.w3.org/2000/svg"
          focusable="false"
          className="block h-auto w-full"
        >
          <circle cx="262" cy="52" r="34" className="fill-accent" />
          <circle cx="40" cy="238" r="14" className="fill-accent" />
          <path
            d="M48 40h150a28 28 0 0 1 28 28v52a28 28 0 0 1-28 28H96l-34 28v-28H48a28 28 0 0 1-28-28V68a28 28 0 0 1 28-28z"
            strokeWidth={4}
            strokeLinejoin="round"
            className="fill-blush stroke-brand"
          />
          <rect x="46" y="68" width="116" height="10" rx="5" className="fill-brand" />
          <rect x="46" y="94" width="80" height="10" rx="5" className="fill-brand" />
          <path
            d="M122 140h150a28 28 0 0 1 28 28v52a28 28 0 0 1-28 28h-14v28l-34-28H122a28 28 0 0 1-28-28v-52a28 28 0 0 1 28-28z"
            className="fill-brand"
          />
          <rect x="120" y="168" width="130" height="10" rx="5" className="fill-canvas" />
          <rect x="120" y="194" width="92" height="10" rx="5" className="fill-canvas" />
        </svg>
      </div>
    </section>
  )
}

export default HeroSection