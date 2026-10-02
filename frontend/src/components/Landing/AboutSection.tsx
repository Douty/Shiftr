function AboutSection() {
  return (
    <section
      aria-labelledby="about-title"
      className="mb-12 rounded-3xl bg-[#E3B5A4] p-7 sm:p-10 lg:p-14"
    >
      <div className="max-w-160">
        <h2
          id="about-title"
          className="mb-4 font-['Fraunces',Georgia,serif] text-[clamp(1.75rem,4vw,2.25rem)] font-semibold leading-[1.15] tracking-[-0.01em]"
        >
          One place for everyone in the building
        </h2>
        <p>
          Shiftr is a property management web app that keeps residents and
          management on the same page. Residents can send requests, report
          issues, and see updates in one place.
        </p>
        <p className="mt-4">
          Property teams can respond, track progress, and share notices without
          chasing phone calls or lost emails. The result is fewer
          misunderstandings, faster fixes, and a home that feels well looked
          after.
        </p>
      </div>
    </section>
  )
}

export default AboutSection