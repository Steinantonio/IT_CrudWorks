import DOMPurify from 'dompurify'
import { marked } from 'marked'
import { useMemo } from 'react'

interface MarkdownProps {
  text: string
  className?: string
}

marked.use({ gfm: true, breaks: true })

/**
 * Renders user-authored markdown. The HTML is sanitized against an allowlist:
 * only a small tag set survives, raw HTML outside it is removed, data/aria
 * attributes are dropped, and links are limited to http(s), mailto, anchors,
 * and same-site paths (ADR-0008).
 */
export default function Markdown({ text, className }: MarkdownProps) {
  const html = useMemo(() => {
    const rendered = marked.parse(text, { async: false })
    return DOMPurify.sanitize(rendered, {
      ALLOWED_TAGS: [
        'p',
        'br',
        'strong',
        'em',
        'del',
        'code',
        'pre',
        'ul',
        'ol',
        'li',
        'blockquote',
        'a',
        'h1',
        'h2',
        'h3',
        'h4',
        'h5',
        'h6',
        'hr',
        'table',
        'thead',
        'tbody',
        'tr',
        'th',
        'td',
      ],
      ALLOWED_ATTR: ['href', 'title'],
      ALLOW_DATA_ATTR: false,
      ALLOW_ARIA_ATTR: false,
      ALLOWED_URI_REGEXP: /^(?:https?:|mailto:|#|\/(?![/\\]))/i,
    })
  }, [text])

  return <div className={className} dangerouslySetInnerHTML={{ __html: html }} />
}
