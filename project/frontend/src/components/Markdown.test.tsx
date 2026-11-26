import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import Markdown from './Markdown'

describe('Markdown', () => {
  it('renders plain text as a paragraph', () => {
    render(<Markdown text="Draft and review the proposal." />)

    expect(screen.getByText('Draft and review the proposal.').tagName).toBe('P')
  })

  it('renders common markdown constructs', () => {
    render(<Markdown text={'**bold** and _italic_\n\n- one\n- two\n\n[link](https://example.com)'} />)

    expect(screen.getByText('bold').tagName).toBe('STRONG')
    expect(screen.getByText('italic').tagName).toBe('EM')
    expect(screen.getAllByRole('listitem')).toHaveLength(2)
    expect(screen.getByRole('link', { name: 'link' })).toHaveAttribute(
      'href',
      'https://example.com',
    )
  })

  it('strips a script tag from the source', () => {
    const { container } = render(<Markdown text={'before <script>alert(1)</script> after'} />)

    expect(container.querySelector('script')).toBeNull()
    expect(container.textContent).not.toContain('alert(1)')
  })

  it('strips event handlers and unsafe elements', () => {
    const { container } = render(
      <Markdown text={'<img src=x onerror="alert(1)"><iframe src="https://evil.test"></iframe>'} />,
    )

    expect(container.querySelector('img')).toBeNull()
    expect(container.querySelector('iframe')).toBeNull()
    expect(container.innerHTML).not.toContain('onerror')
  })

  it('blocks javascript: links but keeps the link text', () => {
    const { container } = render(<Markdown text={'[click](javascript:alert(1))'} />)

    const link = container.querySelector('a')
    expect(link).not.toBeNull()
    expect(link?.getAttribute('href')).toBeNull()
    expect(link?.textContent).toBe('click')
  })

  it('blocks data: links', () => {
    const { container } = render(<Markdown text={'[x](data:text/html,<script>alert(1)</script>)'} />)

    expect(container.querySelector('a')?.getAttribute('href')).toBeNull()
    expect(container.querySelector('script')).toBeNull()
  })

  it('drops data, aria, class, id and style attributes', () => {
    const { container } = render(
      <Markdown
        text={
          '<p data-color-mode="dark" aria-hidden="true" class="x" id="y" style="color:red">text</p>'
        }
      />,
    )

    const paragraph = container.querySelector('p')
    expect(paragraph).not.toBeNull()
    expect(paragraph?.getAttribute('data-color-mode')).toBeNull()
    expect(paragraph?.getAttribute('aria-hidden')).toBeNull()
    expect(paragraph?.getAttribute('class')).toBeNull()
    expect(paragraph?.getAttribute('id')).toBeNull()
    expect(paragraph?.getAttribute('style')).toBeNull()
  })

  it('drops an event handler on an allowed tag', () => {
    const { container } = render(
      <Markdown text={'<a href="https://example.com" onclick="alert(1)">x</a>'} />,
    )

    const link = container.querySelector('a')
    expect(link).toHaveAttribute('href', 'https://example.com')
    expect(link?.getAttribute('onclick')).toBeNull()
  })

  it('removes svg, style, form and srcdoc', () => {
    const { container } = render(
      <Markdown
        text={
          '<svg onload="alert(1)"></svg><style>body{display:none}</style>' +
          '<form action="https://evil.test"><input></form>' +
          '<iframe srcdoc="<script>alert(1)</script>"></iframe>'
        }
      />,
    )

    expect(container.querySelector('svg')).toBeNull()
    expect(container.querySelector('style')).toBeNull()
    expect(container.querySelector('form')).toBeNull()
    expect(container.querySelector('input')).toBeNull()
    expect(container.querySelector('iframe')).toBeNull()
  })

  it('blocks protocol-relative markdown links', () => {
    const { container } = render(<Markdown text={'[a](//evil.test)'} />)

    expect(container.querySelector('a')?.getAttribute('href')).toBeNull()
  })

  it('blocks a raw backslash path that browsers could read as protocol-relative', () => {
    const { container } = render(<Markdown text={'<a href="/\\evil.test">b</a>'} />)

    expect(container.querySelector('a')?.getAttribute('href')).toBeNull()
  })

  it('renders a GFM table', () => {
    render(<Markdown text={'| a | b |\n| - | - |\n| 1 | 2 |'} />)

    expect(screen.getByRole('table')).toBeInTheDocument()
  })
})
