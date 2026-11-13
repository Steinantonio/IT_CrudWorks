import type { SVGProps } from 'react'

type IconProps = SVGProps<SVGSVGElement>

/**
 * Minimal stroke icons for task card metadata. They are purely decorative,
 * so they are hidden from assistive technology and inherit the text colour.
 */
const sharedIconProps = {
  viewBox: '0 0 24 24',
  width: '1rem',
  height: '1rem',
  fill: 'none',
  stroke: 'currentColor',
  strokeWidth: 1.7,
  strokeLinecap: 'round',
  strokeLinejoin: 'round',
  'aria-hidden': true,
  focusable: false,
} as const

export function IconTask(props: IconProps) {
  return (
    <svg {...sharedIconProps} {...props}>
      <path d="M9 5H7a2 2 0 0 0-2 2v12a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2V7a2 2 0 0 0-2-2h-2" />
      <rect x="9" y="3" width="6" height="4" rx="1" />
    </svg>
  )
}

export function IconCalendar(props: IconProps) {
  return (
    <svg {...sharedIconProps} {...props}>
      <rect x="3" y="5" width="18" height="16" rx="2" />
      <path d="M8 3v4M16 3v4M3 10h18" />
    </svg>
  )
}

export function IconDescription(props: IconProps) {
  return (
    <svg {...sharedIconProps} {...props}>
      <path d="M4 6h16M4 12h16M4 18h10" />
    </svg>
  )
}
