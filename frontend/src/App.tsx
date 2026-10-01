import './App.css'
import AboutSection from './components/Landing/AboutSection'
import BenefitsSection from './components/Landing/BenefitsSection'
import HeroSection from './components/Landing/HeroSection'

function App() {
  return (
    <div className="min-h-dvh bg-[#F5E9E2] pb-[env(safe-area-inset-bottom)] pt-[env(safe-area-inset-top)] 
    font-['Inter',system-ui,sans-serif] text-lg leading-relaxed text-[#0B0014] antialiased">
      <a
        href="#main"
        className="sr-only rounded-lg bg-[#0B0014] px-4 py-3 text-[#F5E9E2] focus:not-sr-only focus:absolute focus:left-4 focus:top-4 focus:z-10 focus-visible:outline focus-visible:outline-[3px] focus-visible:outline-offset-[3px] focus-visible:outline-[#0B0014]"
      >
        Skip to main content
      </a>
      <main id="main" className="mx-auto w-full max-w-[72rem] px-5">
        <HeroSection />
        <AboutSection />
        <BenefitsSection />
      </main>
    </div>
  )
}

export default App
